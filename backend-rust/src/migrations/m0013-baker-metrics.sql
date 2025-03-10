CREATE TABLE metrics_bakers
(
    block_height            BIGINT            PRIMARY KEY REFERENCES blocks(height),
    total_bakers_added      BIGINT            NOT NULL,
    total_bakers_removed    BIGINT            NOT NULL,
    total_bakers_resumed    BIGINT            NOT NULL,
    total_bakers_suspended  BIGINT            NOT NULL
);

WITH block_events AS (
  SELECT
    t.block_height,
    COUNT(*) FILTER (WHERE event.elem ? 'BakerRemoved') AS baker_removed_count,
    COUNT(*) FILTER (WHERE event.elem ? 'BakerAdded') AS baker_added_count
  FROM transactions t,
       LATERAL jsonb_array_elements(t.events) AS event(elem)
  GROUP BY t.block_height
  HAVING COUNT(*) FILTER (WHERE event.elem ? 'BakerRemoved') > 0
      OR COUNT(*) FILTER (WHERE event.elem ? 'BakerAdded') > 0
)
SELECT
  block_height,
  SUM(baker_removed_count) OVER (ORDER BY block_height ASC) AS cumulative_baker_removed,
  SUM(baker_added_count) OVER (ORDER BY block_height ASC) AS cumulative_baker_added
FROM block_events
ORDER BY block_height ASC;