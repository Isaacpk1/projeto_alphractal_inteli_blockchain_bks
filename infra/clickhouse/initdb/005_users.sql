CREATE USER IF NOT EXISTS alphractal_api
    IDENTIFIED WITH sha256_password BY 'api_dev_2026';

GRANT SELECT ON alphractal.v_latest_block TO alphractal_api;
GRANT SELECT ON alphractal.v_mempool_now TO alphractal_api;
GRANT SELECT ON alphractal.v_fee_estimates_now TO alphractal_api;
GRANT SELECT ON alphractal.v_eth_fees_1h TO alphractal_api;
GRANT SELECT ON alphractal.v_eth_fees_1d TO alphractal_api;
GRANT SELECT ON alphractal.v_fee_estimates_1d TO alphractal_api;
GRANT SELECT ON alphractal.v_ingestion_status TO alphractal_api;

CREATE USER IF NOT EXISTS alphractal_etl
    IDENTIFIED WITH sha256_password BY 'etl_dev_2026'
    SETTINGS async_insert = 1,
             wait_for_async_insert = 1,
             async_insert_busy_timeout_ms = 1000;

GRANT SELECT, INSERT ON alphractal.eth_blocks TO alphractal_etl;
GRANT SELECT, INSERT ON alphractal.mempool_samples TO alphractal_etl;
GRANT SELECT, INSERT ON alphractal.fee_estimates TO alphractal_etl;
GRANT SELECT, INSERT ON alphractal.eth_usd_prices TO alphractal_etl;
GRANT SELECT, INSERT ON alphractal.ingestion_health TO alphractal_etl;
GRANT SELECT, INSERT ON alphractal.eth_fees_rollup TO alphractal_etl;
GRANT SELECT, INSERT ON alphractal.fee_estimates_1d TO alphractal_etl;
