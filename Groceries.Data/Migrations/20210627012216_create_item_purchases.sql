CREATE OR REPLACE VIEW item_purchases AS
SELECT item_id, transaction_id, created_at, store_id, price, quantity
FROM transaction_items
JOIN transactions USING (transaction_id);
