DO
$$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_database WHERE datname = 'astra') THEN
    CREATE DATABASE astra OWNER astra_owner;
  END IF;
END
$$;
