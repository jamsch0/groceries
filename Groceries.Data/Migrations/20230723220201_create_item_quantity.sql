CREATE OR REPLACE FUNCTION item_quantity(name text) RETURNS jsonb
LANGUAGE SQL
IMMUTABLE
RETURNS NULL ON NULL INPUT
PARALLEL SAFE
AS $$
SELECT jsonb_build_object(
    'amount',
    CASE matches[2]
        WHEN 'k' THEN matches[1]::numeric * 1000
        WHEN 'c' THEN matches[1]::numeric * 100
        WHEN 'm' THEN matches[1]::numeric / 1000
        ELSE matches[1]::numeric
    END,
    'unit',
    CASE matches[3]
        WHEN 'pk' THEN NULL
        ELSE matches[3]
    END,
    'is_metric',
    CASE
        WHEN matches[3] = ANY (ARRAY['g', 'l']) THEN true
        ELSE false
    END,
    'is_divisible',
    CASE
        WHEN matches[3] = ANY (ARRAY['pk', 'sl']) THEN false
        ELSE true
    END
)
FROM regexp_matches(name, '(\d*\.?\d+)\s*(c|k|m)?(g|l|pk|pt|sl)', 'i') AS matches;
$$;
