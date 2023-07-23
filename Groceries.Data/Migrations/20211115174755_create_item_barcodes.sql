CREATE TABLE IF NOT EXISTS item_barcodes (
    item_id uuid NOT NULL REFERENCES items ON DELETE CASCADE,
    barcode_data bigint NOT NULL,
    format text NOT NULL DEFAULT 'unknown',
    PRIMARY KEY (item_id, barcode_data)
);
