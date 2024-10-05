ALTER TABLE item_barcodes
ADD COLUMN IF NOT EXISTS last_scanned_at timestamptz NOT NULL DEFAULT current_timestamp;

UPDATE item_barcodes
SET last_scanned_at = created_at
FROM item_purchases
WHERE item_barcodes.item_id = item_purchases.item_id AND is_last_purchase = true;
