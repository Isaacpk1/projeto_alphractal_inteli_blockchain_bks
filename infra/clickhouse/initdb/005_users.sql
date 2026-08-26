-- 005_users.sql
-- Dois usuários de aplicação, com o mínimo de permissão que cada um precisa.
-- Senhas abaixo são de DESENVOLVIMENTO LOCAL. Nunca reutilize em nuvem.
--
-- Requer CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT=1 (já setado no docker-compose.yml).

-- API .NET: só lê, e só as views da camada de leitura.
CREATE USER IF NOT EXISTS alphractal_api
    IDENTIFIED WITH sha256_password BY 'api_dev_2026';

GRANT SELECT ON alphractal.* TO alphractal_api;

-- ETL Python: escreve nas tabelas base.
-- async_insert liga o buffer do lado do servidor: com um INSERT por bloco
-- (~1 a cada 12 s) e amostras de mempool a cada 1-2 s, sem isto o ClickHouse
-- cria uma part minúscula por INSERT e o merge não acompanha.
CREATE USER IF NOT EXISTS alphractal_etl
    IDENTIFIED WITH sha256_password BY 'etl_dev_2026'
    SETTINGS async_insert = 1,
             wait_for_async_insert = 0,
             async_insert_busy_timeout_ms = 1000;

GRANT SELECT, INSERT ON alphractal.* TO alphractal_etl;
GRANT ALTER UPDATE, ALTER DELETE, OPTIMIZE ON alphractal.* TO alphractal_etl;
