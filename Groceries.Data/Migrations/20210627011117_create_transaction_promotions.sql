CREATE TABLE IF NOT EXISTS transaction_promotions (
    transaction_promotion_id uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    transaction_id uuid NOT NULL REFERENCES transactions ON DELETE CASCADE,
    name text NOT NULL,
    amount numeric(5, 2) NOT NULL CHECK (amount > 0),
    UNIQUE (transaction_id, name)
);

CREATE TABLE IF NOT EXISTS transaction_promotion_items (
    transaction_promotion_id uuid NOT NULL REFERENCES transaction_promotions ON DELETE CASCADE,
    item_id uuid NOT NULL REFERENCES items,
    PRIMARY KEY (transaction_promotion_id, item_id)
);
