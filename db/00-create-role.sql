DO
$$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'astra_owner') THEN
      CREATE ROLE astra_owner LOGIN;
   END IF;

   IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'astra_app') THEN
      CREATE ROLE astra_app LOGIN;
   END IF;
END
$$;
