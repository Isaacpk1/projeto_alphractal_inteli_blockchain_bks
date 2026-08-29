from __future__ import annotations

import json
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from alphractal_etl.contract import ContractError, validate_record


class SpoolError(RuntimeError):
    pass


@dataclass(frozen=True)
class ClaimedFile:
    path: Path
    original_name: str


class Spool:
    def __init__(self, root: Path) -> None:
        self.root = root
        self.ready = root / "ready"
        self.processing = root / "processing"
        self.processed = root / "processed"
        self.failed = root / "failed"

    def ensure_directories(self) -> None:
        for directory in (self.ready, self.processing, self.processed, self.failed):
            directory.mkdir(parents=True, exist_ok=True)

    def claim_all(self) -> list[ClaimedFile]:
        self.ensure_directories()
        claimed = [ClaimedFile(path, path.name) for path in sorted(self.processing.glob("*.ndjson"))]
        for source in sorted(self.ready.glob("*.ndjson")):
            destination = self.processing / source.name
            if destination.exists():
                raise SpoolError(f"arquivo duplicado no processing: {source.name}")
            source.replace(destination)
            claimed.append(ClaimedFile(destination, source.name))
        return claimed

    def read(self, claimed: ClaimedFile) -> dict[str, list[tuple[Any, ...]]]:
        grouped: dict[str, list[tuple[Any, ...]]] = defaultdict(list)
        try:
            with claimed.path.open("r", encoding="utf-8") as stream:
                for line_number, raw_line in enumerate(stream, start=1):
                    if not raw_line.strip():
                        continue
                    try:
                        record = json.loads(raw_line)
                        table, row = validate_record(record)
                    except (json.JSONDecodeError, ContractError) as exc:
                        raise SpoolError(f"{claimed.original_name}:{line_number}: {exc}") from exc
                    grouped[table].append(row)
        except OSError as exc:
            raise SpoolError(f"nao foi possivel ler {claimed.original_name}: {exc}") from exc
        if not grouped:
            raise SpoolError(f"arquivo sem registros: {claimed.original_name}")
        return dict(grouped)

    def complete(self, claimed: ClaimedFile) -> Path:
        destination = self._available_destination(self.processed, claimed.original_name)
        claimed.path.replace(destination)
        return destination

    def reject(self, claimed: ClaimedFile, reason: str) -> Path:
        destination = self._available_destination(self.failed, claimed.original_name)
        claimed.path.replace(destination)
        error_path = destination.with_suffix(destination.suffix + ".error.json")
        error_path.write_text(
            json.dumps({"file": claimed.original_name, "error": reason}, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
        return destination

    @staticmethod
    def _available_destination(directory: Path, name: str) -> Path:
        candidate = directory / name
        counter = 1
        while candidate.exists():
            candidate = directory / f"{Path(name).stem}.{counter}{Path(name).suffix}"
            counter += 1
        return candidate
