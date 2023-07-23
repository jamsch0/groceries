ALTER TABLE items
ADD COLUMN IF NOT EXISTS tags text[] NOT NULL DEFAULT '{}';

CREATE TABLE IF NOT EXISTS item_tags (
    tag text NOT NULL PRIMARY KEY,
    unit_name text NOT NULL
);

INSERT INTO item_tags VALUES ('bacon', 'rashers')
    ON CONFLICT DO NOTHING;
