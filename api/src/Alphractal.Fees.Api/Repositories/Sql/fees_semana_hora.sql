-- Grade dia da semana x hora (heatmap). 1 = segunda ... 7 = domingo.
SELECT dia_semana, hora_utc, amostras, base_fee_gwei_avg
FROM v_fees_semana_hora
ORDER BY dia_semana, hora_utc
