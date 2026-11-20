-- Módulo de Ventas (Maestro-Detalle) - esquema 3FN + CR-01.
-- Idempotente: seguro para ejecutar varias veces sin reintroducir columnas denormalizadas.

CREATE TABLE IF NOT EXISTS productos (
    id     SERIAL PRIMARY KEY,
    nombre VARCHAR(100)  NOT NULL UNIQUE,
    precio NUMERIC(10,2) NOT NULL CHECK (precio >= 0),
    activo BOOLEAN       NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS ventas (
    id         SERIAL PRIMARY KEY,
    fecha      TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    id_cliente INT           NOT NULL REFERENCES clientes(id) ON DELETE RESTRICT,
    total      NUMERIC(10,2) NOT NULL CHECK (total >= 0)
);

CREATE TABLE IF NOT EXISTS detalle_ventas (
    id              SERIAL PRIMARY KEY,
    id_venta        INT           NOT NULL REFERENCES ventas(id) ON DELETE CASCADE,
    id_producto     INT           NOT NULL REFERENCES productos(id) ON DELETE RESTRICT,
    cantidad        INT           NOT NULL CHECK (cantidad > 0),
    precio_unitario NUMERIC(10,2) NOT NULL CHECK (precio_unitario >= 0),
    subtotal        NUMERIC(10,2) NOT NULL CHECK (subtotal = ROUND(cantidad * precio_unitario, 2))
);

INSERT INTO productos (nombre, precio)
SELECT v.nombre, v.precio
FROM (VALUES
    ('Lavado Exterior', 30.00),
    ('Lavado Completo', 60.00),
    ('Encerado y Pulido', 90.00),
    ('Limpieza de Tapiz', 75.00),
    ('Shampoo de Motor', 50.00),
    ('Aromatizacion', 10.00),
    ('Aspirado Interior', 20.00),
    ('Sanitizado Interior', 35.00)
) AS v(nombre, precio)
WHERE NOT EXISTS (SELECT 1 FROM productos);

-- CR-01: usuarios, rol vendedor, columnas de descuento/trazabilidad en ventas
CREATE TABLE IF NOT EXISTS usuarios (
    id                SERIAL PRIMARY KEY,
    empleado_id       INT          NOT NULL UNIQUE REFERENCES empleados(id) ON DELETE RESTRICT,
    nombre_usuario    VARCHAR(50)  NOT NULL UNIQUE,
    contrasena_hash   VARCHAR(255) NOT NULL,
    activo            BOOLEAN      NOT NULL DEFAULT TRUE,
    intentos_fallidos INT          NOT NULL DEFAULT 0 CHECK (intentos_fallidos >= 0),
    bloqueado         BOOLEAN      NOT NULL DEFAULT FALSE,
    ultimo_acceso     TIMESTAMP
);
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS intentos_fallidos INT     NOT NULL DEFAULT 0;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS bloqueado         BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS ultimo_acceso     TIMESTAMP;

INSERT INTO roles (nombre) VALUES ('Vendedor') ON CONFLICT (nombre) DO NOTHING;

ALTER TABLE ventas ADD COLUMN IF NOT EXISTS vendedor              VARCHAR(100)  NOT NULL DEFAULT 'Sin registrar';
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS id_vendedor           INT           REFERENCES empleados(id) ON DELETE RESTRICT;
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS subtotal_bruto        NUMERIC(10,2);
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS porcentaje_descuento  NUMERIC(5,2)  NOT NULL DEFAULT 0;
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS monto_descuento       NUMERIC(10,2) NOT NULL DEFAULT 0;
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS total_final           NUMERIC(10,2);

UPDATE ventas
   SET subtotal_bruto = COALESCE(subtotal_bruto, total),
       total_final    = COALESCE(total_final, total),
       vendedor       = COALESCE(NULLIF(BTRIM(vendedor), ''), 'Sin registrar'),
       porcentaje_descuento = COALESCE(porcentaje_descuento, 0),
       monto_descuento      = COALESCE(monto_descuento, 0)
 WHERE subtotal_bruto IS NULL OR total_final IS NULL OR vendedor IS NULL OR BTRIM(vendedor) = '';

ALTER TABLE ventas ALTER COLUMN subtotal_bruto SET NOT NULL;
ALTER TABLE ventas ALTER COLUMN total_final   SET NOT NULL;

ALTER TABLE ventas DROP CONSTRAINT IF EXISTS ck_ventas_descuento_rango;
ALTER TABLE ventas ADD CONSTRAINT ck_ventas_descuento_rango
    CHECK (porcentaje_descuento >= 0 AND porcentaje_descuento <= 20);
ALTER TABLE ventas DROP CONSTRAINT IF EXISTS ck_ventas_montos_coherentes;
ALTER TABLE ventas ADD CONSTRAINT ck_ventas_montos_coherentes CHECK (
    subtotal_bruto >= 0
    AND monto_descuento = ROUND(subtotal_bruto * porcentaje_descuento / 100, 2)
    AND total_final     = subtotal_bruto - monto_descuento
    AND total           = total_final
);
ALTER TABLE ventas DROP CONSTRAINT IF EXISTS ck_ventas_vendedor;
ALTER TABLE ventas ADD CONSTRAINT ck_ventas_vendedor CHECK (
    LENGTH(BTRIM(vendedor)) > 0
    AND (porcentaje_descuento = 0 OR vendedor <> 'Sin registrar')
);

CREATE INDEX IF NOT EXISTS idx_ventas_vendedor    ON ventas (vendedor);
CREATE INDEX IF NOT EXISTS idx_ventas_id_vendedor ON ventas (id_vendedor);
