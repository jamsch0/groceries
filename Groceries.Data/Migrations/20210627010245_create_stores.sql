CREATE TABLE IF NOT EXISTS retailers (
    retailer_id uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    name text NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS stores (
    store_id uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    retailer_id uuid NOT NULL REFERENCES retailers,
    name text NOT NULL,
    address text,
    UNIQUE (retailer_id, name)
);
