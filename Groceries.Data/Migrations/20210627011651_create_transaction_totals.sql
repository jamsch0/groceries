CREATE OR REPLACE VIEW transaction_totals AS
SELECT transaction_id, sum(amount) AS total
FROM (
    SELECT transaction_id, price * quantity AS amount
    FROM transaction_items
    UNION ALL
    SELECT transaction_id, -amount
    FROM transaction_promotions
) AS transaction_amounts
GROUP BY transaction_id;
