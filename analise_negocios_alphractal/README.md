# Pacote de Análise de Negócios — Alphractal | Módulo "Fees"

Documentação de negócio do projeto de desenvolvimento do módulo de rastreamento de taxas de rede (gas) para a plataforma Alphractal.

## Documentos

| # | Documento | Conteúdo |
|---|---|---|
| 00 | [`alphractal-introducao-parceiro-swot.md`](./alphractal-introducao-parceiro-swot.md) | Introdução do parceiro + Matriz SWOT + cruzamento TOWS |
| 01 | [`01-brd-modulo-fees.md`](./01-brd-modulo-fees.md) | **BRD** — documento guarda-chuva: problema, objetivos, escopo, stakeholders, requisitos, critérios de sucesso |
| 02 | [`02-business-model-canvas.md`](./02-business-model-canvas.md) | Business Model Canvas da Alphractal e encaixe do módulo |
| 03 | [`03-analise-mercado.md`](./03-analise-mercado.md) | 5 Forças de Porter + PESTEL + Matriz Oceano Azul |
| 04 | [`04-benchmarking-competitivo.md`](./04-benchmarking-competitivo.md) | Comparativo funcional dos gas trackers e plataformas de analytics |
| 05 | [`05-matriz-de-risco.md`](./05-matriz-de-risco.md) | Matriz probabilidade × impacto e planos de resposta |
| 06 | [`06-analise-financeira-tco.md`](./06-analise-financeira-tco.md) | TCO de infraestrutura, break-even e análise de sensibilidade |

---

## ⚠️ Fato de mercado que atravessa todos os documentos

**A Blocknative encerrou suas operações em 19 de junho de 2026.**

A empresa era, desde 2018, a referência de mercado em previsão de gas fee e monitoramento de mempool — sua Gas Platform e a Gas Network alimentavam carteiras, dApps e bots em dezenas de redes. Em maio de 2026, a Deloitte fez uma aquisição de talentos ("acqui-hire") do time, e a companhia optou por **desligar as APIs em vez de vendê-las** a outro provedor de infraestrutura.

Isso tem três consequências diretas para este projeto e aparece em todos os documentos:

1. **Oportunidade** — abriu-se um vácuo de oferta em dados de gas de qualidade institucional, com uma base de usuários forçada a migrar em janela curta.
2. **Alerta estratégico** — a escolha de desligar em vez de vender sinaliza que o *dado bruto* de gas, isolado, é difícil de monetizar. Isso valida a tese central do projeto: o valor precisa estar na camada de interpretação e integração, não no dado.
3. **Restrição técnica** — a Blocknative deixa de ser opção de fornecedor. A arquitetura deve assumir coleta própria via RPC + fallback multi-provedor.

---

## Como usar este pacote

- O **BRD (01)** é o documento guarda-chuva. Os demais o alimentam ou detalham.
- Os documentos **00, 03 e 04** compõem o item "análise de mercado" da Semana 1.
- Os documentos **05 e 06** dependem de validações técnicas com o parceiro (provedor de RPC, limites de plano, ambiente de deploy) — as lacunas estão marcadas com ⚠️ ao longo do texto.

## Convenções

- ⚠️ = premissa não confirmada, exige validação com o parceiro
- IDs de requisito: `RN-` (negócio), `RF-` (funcional), `RNF-` (não funcional), `RB-` (regra de negócio)
- IDs de risco: `R-##`
- Valores de infraestrutura em USD (moeda de cobrança dos provedores); converter pelo câmbio vigente na data da apresentação

---

*Elaborado em agosto de 2026. Dados de preço e oferta de concorrentes mudam com frequência — reconfirmar antes da entrega final.*
