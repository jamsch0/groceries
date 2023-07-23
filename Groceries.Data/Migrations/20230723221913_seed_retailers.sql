INSERT INTO retailers VALUES
    (DEFAULT, 'ALDI'),
    (DEFAULT, 'ASDA'),
    (DEFAULT, 'Lidl'),
    (DEFAULT, 'Morrisons'),
    (DEFAULT, 'Sainsbury''s'),
    (DEFAULT, 'Tesco')
    ON CONFLICT DO NOTHING;
