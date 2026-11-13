-- Ejecutar una vez en la base de datos "pepokita" (pgAdmin o psql)
CREATE TABLE IF NOT EXISTS clientes (
    id        SERIAL PRIMARY KEY,
    documento VARCHAR(20)  NOT NULL UNIQUE,
    nombre    VARCHAR(80)  NOT NULL,
    apellido  VARCHAR(80)  NOT NULL,
    telefono  VARCHAR(20)  NOT NULL,
    correo    VARCHAR(120),
    direccion TEXT,
    activo    BOOLEAN      NOT NULL DEFAULT TRUE
);
