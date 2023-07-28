CREATE OR REPLACE VIEW item_purchases AS
SELECT
    item_id,
    transaction_id,
    created_at,
    store_id,
    price,
    quantity,
    CASE ROW_NUMBER() OVER (PARTITION BY item_id ORDER BY created_at DESC)
        WHEN 1 THEN true
        ELSE false
    END AS is_last_purchase
FROM transaction_items
JOIN transactions USING (transaction_id);
