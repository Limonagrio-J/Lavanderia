-- Módulo de Ventas (Maestro-Detalle). La aplicación ejecuta esto sola al iniciar,
-- este archivo queda como referencia / para correrlo a mano si se prefiere.

CREATE TABLE IF NOT EXISTS productos (
    id     SERIAL PRIMARY KEY,
    nombre VARCHAR(100)  NOT NULL,
    precio NUMERIC(10,2) NOT NULL CHECK (precio >= 0),
    activo BOOLEAN       NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS ventas (
    id      SERIAL PRIMARY KEY,
    fecha   TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    cliente VARCHAR(150)  NOT NULL,
    total   NUMERIC(10,2) NOT NULL
);

CREATE TABLE IF NOT EXISTS detalle_ventas (
    id              SERIAL PRIMARY KEY,
    id_venta        INT           NOT NULL REFERENCES ventas(id) ON DELETE CASCADE,
    id_producto     INT           NOT NULL, -- Referencia a la tabla productos
    nombre_producto VARCHAR(100)  NOT NULL,
    cantidad        INT           NOT NULL CHECK (cantidad > 0),
    precio_unitario NUMERIC(10,2) NOT NULL,
    subtotal        NUMERIC(10,2) NOT NULL
);
