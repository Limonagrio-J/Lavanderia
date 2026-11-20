-- OPCIONAL: la aplicación ejecuta esto automáticamente al iniciar.
-- Solo necesitas crear antes la base de datos (por defecto "Pepokita") en pgAdmin o psql.
CREATE TABLE IF NOT EXISTS clientes (
    id             SERIAL PRIMARY KEY,
    documento      VARCHAR(20)  NOT NULL UNIQUE,
    nombre         VARCHAR(80)  NOT NULL,
    apellido       VARCHAR(80)  NOT NULL,
    telefono       VARCHAR(20)  NOT NULL,
    correo         VARCHAR(120),
    direccion      TEXT,
    activo         BOOLEAN      NOT NULL DEFAULT TRUE,
    tipo_cliente   VARCHAR(20)  NOT NULL DEFAULT 'Particular',
    fecha_registro DATE         NOT NULL DEFAULT CURRENT_DATE
);

-- Si tu tabla ya existía sin las dos últimas columnas:
ALTER TABLE clientes ADD COLUMN IF NOT EXISTS tipo_cliente VARCHAR(20) NOT NULL DEFAULT 'Particular';
ALTER TABLE clientes ADD COLUMN IF NOT EXISTS fecha_registro DATE NOT NULL DEFAULT CURRENT_DATE;
