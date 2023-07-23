CREATE TABLE IF NOT EXISTS transactions (
    transaction_id uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    created_at timestamptz NOT NULL DEFAULT current_timestamp,
    store_id uuid NOT NULL REFERENCES stores
);

CREATE TABLE IF NOT EXISTS transaction_items (
    transaction_id uuid NOT NULL REFERENCES transactions ON DELETE CASCADE,
    item_id uuid NOT NULL REFERENCES items,
    price numeric(5, 2) NOT NULL CHECK (price >= 0),
    quantity integer NOT NULL CHECK (quantity > 0),
    PRIMARY KEY (transaction_id, item_id)
);
