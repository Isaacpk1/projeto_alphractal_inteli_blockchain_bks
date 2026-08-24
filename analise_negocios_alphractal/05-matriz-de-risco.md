# Matriz de Risco — Módulo "Fees"

---

## 1. Metodologia

Cada risco é avaliado em **probabilidade (P)** e **impacto (I)**, ambos em escala de 1 a 5. O **grau de exposição** é o produto P × I.

| Escala | Probabilidade | Impacto |
|:---:|---|---|
| 1 | Muito baixa — improvável no horizonte do projeto | Insignificante — absorvido sem ajuste |
| 2 | Baixa — pode ocorrer em circunstância específica | Menor — ajuste pontual de cronograma |
| 3 | Média — provável ao menos uma vez | Moderado — replanejamento de sprint |
| 4 | Alta — esperado que ocorra | Alto — compromete entrega parcial |
| 5 | Muito alta — praticamente certo | Crítico — inviabiliza o projeto |

| Faixa de exposição | Classificação | Tratamento |
|:---:|---|---|
| 1–4 | 🟢 Baixo | Aceitar e monitorar |
| 5–9 | 🟡 Moderado | Mitigar com ação planejada |
| 10–14 | 🟠 Alto | Mitigar com ação prioritária e responsável nomeado |
| 15–25 | 🔴 Crítico | Ação imediata; escalar ao patrocinador |

Estratégias de resposta: **Mitigar**, **Transferir**, **Evitar**, **Aceitar**.

---

## 2. Registro de riscos

### Riscos técnicos e de infraestrutura

| ID | Risco | P | I | P×I | Classe | Estratégia | Resposta |
|---|---|:---:|:---:|:---:|:---:|---|---|
| **R-01** | Estouro do limite de requisições ou de unidades de computação do provedor RPC, interrompendo a coleta | 4 | 4 | **16** | 🔴 | Mitigar | Instrumentar consumo desde o primeiro dia; alarme em 70% da cota; usar assinatura WebSocket em vez de polling; amostragem adaptativa por rede |
| **R-02** | Perda ou degradação da conexão WebSocket, gerando lacunas silenciosas na série | 4 | 3 | **12** | 🟠 | Mitigar | Reconexão automática com backoff; detector de lacuna por número de bloco; rotina de backfill automática após reconexão |
| **R-03** | Reorganização de cadeia invalidando blocos já persistidos | 3 | 3 | **9** | 🟡 | Mitigar | Marcar blocos como provisórios até N confirmações (RB-02); rotina de reconciliação |
| **R-06** | Descontinuação ou mudança de política de um provedor de dados | 3 | 4 | **12** | 🟠 | Mitigar | Camada de abstração de provedor; fallback configurado e testado; nunca depender de fornecedor único **(risco materializado no setor: Blocknative, jun/2026)** |
| **R-12** | Indisponibilidade ou custo proibitivo de dados históricos para backfill | 3 | 3 | **9** | 🟡 | Mitigar | Definir profundidade histórica mínima aceitável na Sprint 1; backfill incremental em horário de baixa; considerar fonte alternativa para histórico profundo |
| **R-14** | Crescimento do volume de dados degradando desempenho de consulta | 3 | 2 | **6** | 🟡 | Mitigar | Agregações pré-calculadas por janela; política de retenção por granularidade |

### Riscos financeiros

| ID | Risco | P | I | P×I | Classe | Estratégia | Resposta |
|---|---|:---:|:---:|:---:|:---:|---|---|
| **R-04** | Custo de infraestrutura acima do orçado, sobretudo ao adicionar redes de bloco rápido | 4 | 3 | **12** | 🟠 | Mitigar | Modelagem de custo antes de cada nova rede (doc. 06); teto de gasto configurado no provedor; revisão semanal do consumo real |
| **R-15** | Variação cambial elevando custo em real de serviços cobrados em dólar | 3 | 2 | **6** | 🟡 | Aceitar | Monitorar; sem ação no horizonte do projeto |

### Riscos de produto e mercado

| ID | Risco | P | I | P×I | Classe | Estratégia | Resposta |
|---|---|:---:|:---:|:---:|:---:|---|---|
| **R-07** | Usuário percebe o módulo como redundante frente a alternativas gratuitas | 4 | 4 | **16** | 🔴 | Evitar | Não competir no dado bruto; priorizar métricas proprietárias e visualização cruzada desde o MVP; validar percepção de valor com usuário real antes da Sprint 4 |
| **R-05** | Mudança de protocolo alterando a semântica das taxas e quebrando séries | 2 | 4 | **8** | 🟡 | Mitigar | Versionar metodologia (RNF-06); anotar quebras na série (RB-05); acompanhar propostas de melhoria do Ethereum |
| **R-10** | Retração do mercado cripto reduzindo prioridade e orçamento do módulo | 3 | 3 | **9** | 🟡 | Aceitar | Fora do controle do projeto; entregar valor em horizonte curto reduz exposição |
| **R-16** | Concorrente lança módulo equivalente durante o desenvolvimento | 2 | 3 | **6** | 🟡 | Mitigar | Velocidade de entrega; diferenciar por integração, não por funcionalidade isolada |

### Riscos de projeto e execução

| ID | Risco | P | I | P×I | Classe | Estratégia | Resposta |
|---|---|:---:|:---:|:---:|:---:|---|---|
| **R-08** | Inflação de escopo comprometendo a entrega em 10 semanas | 4 | 4 | **16** | 🔴 | Evitar | Escopo congelado ao fim da Sprint 1; toda inclusão exige remoção equivalente; lista "fora do escopo" do BRD como referência formal |
| **R-09** | Atraso na liberação de credenciais ou acesso a ambiente pelo parceiro | 3 | 5 | **15** | 🔴 | Mitigar | Escalar na primeira semana; plano B com camada gratuita de provedor público para não bloquear as Sprints 1 e 2 |
| **R-11** | Concentração de conhecimento em um único integrante | 3 | 3 | **9** | 🟡 | Mitigar | Revisão cruzada de código; documentação contínua; rodízio de responsabilidade por componente |
| **R-13** | Definição tardia das redes prioritárias gerando retrabalho de arquitetura | 3 | 3 | **9** | 🟡 | Mitigar | Arquitetura agnóstica de rede desde o início; decisão formal exigida até o fim da Sprint 1 |
| **R-17** | Divergência entre expectativa do parceiro e escopo acordado | 2 | 4 | **8** | 🟡 | Mitigar | Demonstração ao fim de cada sprint; critérios de aceite assinados no início |

### Riscos legais e de conformidade

| ID | Risco | P | I | P×I | Classe | Estratégia | Resposta |
|---|---|:---:|:---:|:---:|:---:|---|---|
| **R-18** | Restrição contratual do provedor à redistribuição de dados via API paga | 2 | 3 | **6** | 🟡 | Mitigar | Revisar termos de uso antes de expor endpoint público; expor métrica derivada em vez de dado bruto reduz o risco |
| **R-19** | Evolução do módulo para rastreamento de endereços individuais criando exposição sob a LGPD | 1 | 4 | **4** | 🟢 | Evitar | Manter o escopo em métricas agregadas; qualquer evolução nesse sentido exige parecer jurídico prévio |

---

## 3. Mapa de calor

```
        │  1        2        3        4        5
   ─────┼──────────────────────────────────────────
    5   │                   R-09
        │                   🔴15
   ─────┼──────────────────────────────────────────
 I  4   │        R-05      R-06     R-01
 M      │        R-17      R-04*    R-07
 P      │        🟡8       R-10*    R-08
 A      │                  🟠/🟡    🔴16
   ─────┼──────────────────────────────────────────
    3   │        R-18      R-03     R-02
        │        R-16      R-11     🟠12
        │        🟡6       R-12
        │                  R-13 🟡9
   ─────┼──────────────────────────────────────────
    2   │                  R-14
        │                  R-15 🟡6
   ─────┼──────────────────────────────────────────
    1   │  R-19 🟢4
   ─────┴──────────────────────────────────────────
                    PROBABILIDADE
```
*R-04 tem I=3 e P=4 (🟠12); R-10 tem I=3 e P=3 (🟡9) — posicionados na faixa por proximidade visual.*

---

## 4. Riscos críticos — plano de ação detalhado

### 🔴 R-01 — Estouro de limite do provedor RPC (16)

**Por que é o risco número um:** é o único que interrompe a coleta por completo, e a probabilidade é alta porque a intuição sobre consumo costuma subestimar o custo real de coleta em alta frequência.

**Ação preventiva**
1. Modelar o consumo esperado **antes** de escrever o coletor (doc. 06)
2. Preferir assinatura de novos blocos via WebSocket a polling — a diferença de consumo é de ordem de grandeza
3. Instrumentar consumo por endpoint desde o primeiro commit
4. Configurar alarme em 70% da cota mensal

**Gatilho de escalonamento:** consumo projetado ultrapassando 80% da cota antes do dia 20 do mês.

**Plano de contingência:** degradar granularidade automaticamente (de por bloco para amostragem) antes de perder a coleta inteira.

---

### 🔴 R-07 — Percepção de comoditização (16)

**Por que é crítico:** é o risco que anula o valor de negócio mesmo com execução técnica impecável. Se o usuário olha o módulo e pensa "isso eu vejo de graça no Etherscan", o projeto entregou software sem entregar valor.

**Evidência de que o risco é real:** a Blocknative tinha a melhor tecnologia de estimativa de gas do mercado e ainda assim encerrou operações em junho de 2026, sem encontrar comprador para a infraestrutura.

**Ação preventiva**
1. Nenhuma tela do módulo deve mostrar apenas o gas atual sem contexto
2. Ao menos uma visualização cruzada entregue já na Sprint 3, não na 5
3. Teste de percepção com usuário real antes da Sprint 4
4. Métricas proprietárias como argumento central da demo, não como complemento

**Indicador de alerta:** se, na demo intermediária, o parceiro descrever o módulo como "gas tracker", o posicionamento falhou.

---

### 🔴 R-08 — Inflação de escopo (16)

**Por que é crítico:** dez semanas com equipe de dedicação parcial é um envelope apertado, e a natureza do tema convida a expansão — mais redes, mais métricas, previsão, mempool.

**Ação preventiva**
1. Escopo congelado por escrito ao fim da Sprint 1
2. Regra de troca: toda inclusão exige remoção de item de esforço equivalente
3. Lista de "fora do escopo" do BRD tratada como documento formal, revisada em cada sprint
4. Backlog de fase 2 explícito, para acolher boas ideias sem absorvê-las agora

---

### 🔴 R-09 — Atraso na liberação de acessos pelo parceiro (15)

**Por que é crítico:** impacto 5 porque bloqueia integralmente as Sprints 2 e 3. Não há trabalho técnico significativo possível sem acesso a dados.

**Ação preventiva**
1. Solicitar credenciais na primeira reunião, não na primeira necessidade
2. Definir prazo-limite explícito e responsável nomeado do lado do parceiro
3. Escalar ao patrocinador se não resolvido em cinco dias úteis

**Plano B:** iniciar com camada gratuita de provedor público em nome da equipe. Sacrifica limites de throughput, mas mantém o cronograma. **Este plano B deve ser preparado na Sprint 1, não acionado na Sprint 2.**

---

## 5. Governança de riscos

| Item | Definição |
|---|---|
| Revisão da matriz | Ao início de cada sprint |
| Responsável pela matriz | Líder técnico da equipe |
| Escalonamento ao parceiro | Qualquer risco que entre na faixa 🔴, ou 🟠 que se materialize |
| Registro de materialização | Todo risco que ocorrer é documentado com data, impacto real e resposta aplicada |
| Novos riscos | Podem ser adicionados a qualquer momento; a matriz é documento vivo |

---

## 6. Resumo executivo

**19 riscos mapeados.** Distribuição: 4 críticos, 3 altos, 10 moderados, 2 baixos.

**Concentração relevante:** três dos quatro riscos críticos são de **execução e posicionamento**, não de tecnologia. O risco técnico dominante (R-01) é conhecido, mensurável e mitigável por arquitetura.

Em outras palavras: a maior ameaça a este projeto não é não conseguir coletar o dado. É coletar o dado corretamente e entregar algo que o usuário já tem de graça em outro lugar.
