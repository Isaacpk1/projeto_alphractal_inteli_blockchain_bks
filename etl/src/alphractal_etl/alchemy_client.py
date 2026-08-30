from __future__ import annotations

import itertools
import random
import time
from typing import Any

import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry


class AlchemyError(RuntimeError):
    pass


class RateLimitError(AlchemyError):
    """Estouro de throughput (CU/s), nao de cota mensal.

    Merece classe propria porque e a unica falha da Alchemy que se resolve
    apenas esperando: nem o intervalo, nem a chave, nem o payload estao errados.
    """


#: Numero de tentativas por chamada antes de desistir.
MAX_TENTATIVAS = 6

#: Espera inicial do backoff, em segundos. Dobra a cada tentativa.
ESPERA_INICIAL = 1.0


def _e_rate_limit(erro: Any) -> bool:
    """Um erro JSON-RPC e estouro de throughput?

    O 429 do batch NAO chega como status HTTP: a resposta vem 200 e cada item
    carrega o proprio erro. Por isso o Retry do urllib3, que olha o status, nunca
    disparava aqui — e o backfill morria na primeira rajada.
    """
    if not isinstance(erro, dict):
        return False
    if erro.get("code") == 429:
        return True
    return "compute units" in str(erro.get("message", "")).lower()


class AlchemyClient:
    def __init__(
        self,
        api_key: str,
        timeout_seconds: float = 30,
        intervalo_minimo: float = 0.0,
    ) -> None:
        if not api_key.strip():
            raise ValueError("ALCHEMY_API_KEY nao configurada")
        self._url = f"https://eth-mainnet.g.alchemy.com/v2/{api_key.strip()}"
        self._timeout = timeout_seconds
        # Ritmo minimo entre requisicoes. O limite da Alchemy e de unidades por
        # SEGUNDO: sem espacar, uma rajada estoura mesmo com poucos lotes.
        self._intervalo_minimo = max(0.0, intervalo_minimo)
        self._ultima_requisicao = 0.0
        self._ids = itertools.count(1)
        self._session = requests.Session()
        retries = Retry(
            total=5,
            backoff_factor=0.5,
            status_forcelist=(429, 500, 502, 503, 504),
            allowed_methods=frozenset({"POST"}),
            respect_retry_after_header=True,
        )
        self._session.mount("https://", HTTPAdapter(max_retries=retries))

    def close(self) -> None:
        self._session.close()

    def get_block_number(self) -> int:
        return int(self._rpc("eth_blockNumber"), 16)

    def get_fee_history(
        self,
        block_count: int,
        newest_block: int,
        reward_percentiles: list[int],
    ) -> dict[str, Any]:
        if block_count <= 0:
            raise ValueError("block_count deve ser positivo")
        result = self._rpc(
            "eth_feeHistory",
            [hex(block_count), hex(newest_block), reward_percentiles],
        )
        parsed: dict[str, Any] = {
            "oldest_block": int(result["oldestBlock"], 16),
            "base_fee_per_gas": [int(value, 16) for value in result["baseFeePerGas"]],
            "gas_used_ratio": [float(value) for value in result["gasUsedRatio"]],
            "reward": [[int(value, 16) for value in row] for row in result.get("reward", [])],
        }
        returned_blocks = len(parsed["gas_used_ratio"])
        if len(parsed["base_fee_per_gas"]) != returned_blocks + 1:
            raise AlchemyError("eth_feeHistory retornou baseFeePerGas inconsistente")
        if parsed["reward"] and len(parsed["reward"]) != returned_blocks:
            raise AlchemyError("eth_feeHistory retornou reward inconsistente")
        return parsed

    def get_blocks(self, block_numbers: list[int]) -> list[dict[str, Any]]:
        if not block_numbers:
            return []
        requests_by_id: dict[int, int] = {}
        batch = []
        for number in block_numbers:
            request_id = next(self._ids)
            requests_by_id[request_id] = number
            batch.append({
                "jsonrpc": "2.0", "id": request_id, "method": "eth_getBlockByNumber",
                "params": [hex(number), False],
            })
        payload = self._post_com_retry(batch)
        if not isinstance(payload, list):
            raise AlchemyError("resposta batch invalida")
        results: dict[int, dict[str, Any]] = {}
        for item in payload:
            if not isinstance(item, dict) or "id" not in item:
                raise AlchemyError("item batch invalido")
            request_id = int(item["id"])
            expected_number = requests_by_id.get(request_id)
            if expected_number is None:
                raise AlchemyError(f"id batch desconhecido: {request_id}")
            if "error" in item:
                raise AlchemyError(f"RPC falhou para o bloco {expected_number}: {item['error']}")
            raw = item.get("result")
            if raw is None:
                raise AlchemyError(f"bloco nao encontrado: {expected_number}")
            number = int(raw["number"], 16)
            if number != expected_number:
                raise AlchemyError(f"bloco inesperado: esperado {expected_number}, recebido {number}")
            results[number] = {
                "number": number, "hash": raw["hash"],
                "timestamp": int(raw["timestamp"], 16),
                "base_fee_per_gas": int(raw["baseFeePerGas"], 16),
                "gas_used": int(raw["gasUsed"], 16),
                "gas_limit": int(raw["gasLimit"], 16),
                "tx_count": len(raw["transactions"]),
            }
        missing = sorted(set(block_numbers) - set(results))
        if missing:
            raise AlchemyError(f"blocos ausentes no batch: {missing}")
        return [results[number] for number in block_numbers]

    def _post_com_retry(self, payload: Any) -> Any:
        """POST com backoff exponencial quando a resposta indica throughput estourado.

        O batch inteiro e refeito, nao apenas os itens que falharam: a Alchemy
        pode recusar itens diferentes a cada tentativa, e reconstruir o batch
        parcial complicaria o mapeamento de ids sem ganho real.
        """
        espera = ESPERA_INICIAL
        for tentativa in range(1, MAX_TENTATIVAS + 1):
            resposta = self._post(payload)

            if isinstance(resposta, list):
                limitado = any(_e_rate_limit(item.get("error")) for item in resposta if isinstance(item, dict))
            elif isinstance(resposta, dict):
                limitado = _e_rate_limit(resposta.get("error"))
            else:
                limitado = False

            if not limitado:
                return resposta

            if tentativa == MAX_TENTATIVAS:
                raise RateLimitError(
                    f"throughput da Alchemy estourado apos {MAX_TENTATIVAS} tentativas. "
                    "Reduza --batch-size ou aumente --pausa-lote"
                )

            # Jitter evita que varias execucoes simultaneas voltem em sincronia
            # e estourem o limite juntas de novo.
            time.sleep(espera + random.uniform(0, espera * 0.25))
            espera *= 2
        raise RateLimitError("throughput da Alchemy estourado")

    def _rpc(self, method: str, params: list[Any] | None = None) -> Any:
        request_id = next(self._ids)
        body = self._post_com_retry({
            "jsonrpc": "2.0", "id": request_id, "method": method, "params": params or [],
        })
        if not isinstance(body, dict):
            raise AlchemyError("resposta RPC invalida")
        if "error" in body:
            raise AlchemyError(f"RPC {method} falhou: {body['error']}")
        if "result" not in body:
            raise AlchemyError(f"RPC {method} sem result")
        return body["result"]

    def _post(self, payload: Any) -> Any:
        if self._intervalo_minimo > 0:
            desde_a_ultima = time.monotonic() - self._ultima_requisicao
            if desde_a_ultima < self._intervalo_minimo:
                time.sleep(self._intervalo_minimo - desde_a_ultima)
            self._ultima_requisicao = time.monotonic()
        try:
            response = self._session.post(self._url, json=payload, timeout=self._timeout)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as exc:
            status = exc.response.status_code if exc.response is not None else "sem resposta"
            raise AlchemyError(f"falha HTTP na Alchemy (status={status})") from None
        except ValueError:
            raise AlchemyError("Alchemy retornou JSON invalido") from None
