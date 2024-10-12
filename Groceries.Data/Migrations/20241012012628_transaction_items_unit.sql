DROP VIEW item_purchases;
DROP VIEW transaction_totals;

ALTER TABLE transaction_items
    ALTER COLUMN quantity TYPE numeric(5, 3);

ALTER TABLE transaction_items
    ADD COLUMN IF NOT EXISTS unit text;

CREATE VIEW item_purchases AS
SELECT
    item_id,
    transaction_id,
    created_at,
    store_id,
    price,
    quantity,
    unit,
    CASE ROW_NUMBER() OVER (PARTITION BY item_id ORDER BY created_at DESC)
        WHEN 1 THEN true
        ELSE false
    END AS is_last_purchase
FROM transaction_items
JOIN transactions USING (transaction_id);

CREATE VIEW transaction_totals AS
SELECT transaction_id, sum(amount) AS total
FROM (
    SELECT transaction_id, price * quantity AS amount
    FROM transaction_items
    UNION ALL
    SELECT transaction_id, -amount
    FROM transaction_promotions
) AS transaction_amounts
GROUP BY transaction_id;
