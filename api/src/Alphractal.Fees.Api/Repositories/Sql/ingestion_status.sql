-- Heartbeat por componente (ws_listener, api, etl). Alimenta /api/v1/status.
SELECT
    component,
    status,
    lag_ms,
    last_block,
    detail,
    last_seen_at
FROM v_ingestion_status
ORDER BY component
