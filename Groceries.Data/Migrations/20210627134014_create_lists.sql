CREATE TABLE IF NOT EXISTS lists (
    list_id uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    updated_at timestamptz NOT NULL DEFAULT current_timestamp,
    name text NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS list_items (
    list_item_id uuid NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    list_id uuid NOT NULL REFERENCES lists ON DELETE CASCADE,
    name text NOT NULL,
    completed boolean NOT NULL,
    UNIQUE (list_id, name)
);
