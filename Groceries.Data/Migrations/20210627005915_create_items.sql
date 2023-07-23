CREATE TABLE IF NOT EXISTS items (
    item_id uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    updated_at timestamptz NOT NULL DEFAULT current_timestamp,
    brand text NOT NULL,
    name text NOT NULL,
    UNIQUE (brand, name)
);
