-- cr01_descuentos_vendedor.sql
-- Cambios CR-01: Control de Descuentos y Trazabilidad (idempotente)
BEGIN;

-- 1) Tabla usuarios (1:1 con empleados) - crear o completar
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
ALTER TABLE usuarios DROP CONSTRAINT IF EXISTS usuarios_intentos_fallidos_check;
ALTER TABLE usuarios ADD CONSTRAINT usuarios_intentos_fallidos_check CHECK (intentos_fallidos >= 0);

-- 2) Rol Vendedor
INSERT INTO roles (nombre) VALUES ('Vendedor') ON CONFLICT (nombre) DO NOTHING;

-- 3) Columnas nuevas en ventas
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS vendedor              VARCHAR(100)  NOT NULL DEFAULT 'Sin registrar';
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS id_vendedor           INT           REFERENCES empleados(id) ON DELETE RESTRICT;
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS subtotal_bruto        NUMERIC(10,2);
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS porcentaje_descuento  NUMERIC(5,2)  NOT NULL DEFAULT 0;
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS monto_descuento       NUMERIC(10,2) NOT NULL DEFAULT 0;
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS total_final           NUMERIC(10,2);

-- 4) Backfill de ventas existentes
UPDATE ventas
   SET subtotal_bruto = COALESCE(subtotal_bruto, total),
       total_final    = COALESCE(total_final, total),
       vendedor       = COALESCE(NULLIF(BTRIM(vendedor), ''), 'Sin registrar'),
       porcentaje_descuento = COALESCE(porcentaje_descuento, 0),
       monto_descuento      = COALESCE(monto_descuento, 0)
 WHERE subtotal_bruto IS NULL
    OR total_final IS NULL
    OR vendedor IS NULL
    OR BTRIM(vendedor) = '';

ALTER TABLE ventas ALTER COLUMN subtotal_bruto SET NOT NULL;
ALTER TABLE ventas ALTER COLUMN total_final   SET NOT NULL;
ALTER TABLE ventas ALTER COLUMN vendedor       SET NOT NULL;
ALTER TABLE ventas ALTER COLUMN porcentaje_descuento SET NOT NULL;
ALTER TABLE ventas ALTER COLUMN monto_descuento      SET NOT NULL;

-- 5) Restricciones
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

-- 6) Indices de auditoria
CREATE INDEX IF NOT EXISTS idx_ventas_vendedor    ON ventas (vendedor);
CREATE INDEX IF NOT EXISTS idx_ventas_id_vendedor ON ventas (id_vendedor);

-- 7) Seed usuario inicial (vendedor) para login
INSERT INTO empleados (rol_id, documento, nombre, apellido, telefono, activo)
SELECT r.id, 'VEN-0001', 'Carlos', 'Perez', '70000001', TRUE
  FROM roles r WHERE r.nombre = 'Vendedor'
ON CONFLICT (documento) DO NOTHING;

INSERT INTO usuarios (empleado_id, nombre_usuario, contrasena_hash, activo, intentos_fallidos, bloqueado)
SELECT e.id, 'cperez', 'v1$JU6Ff5stfcumJuRafzaB2Q==$pOnR6xCWUYsaLgtoDEONPxrl1xxRZVcTKB6Aa6V87Lg=', TRUE, 0, FALSE
  FROM empleados e WHERE e.documento = 'VEN-0001'
ON CONFLICT (nombre_usuario) DO NOTHING;

SELECT setval('usuarios_id_seq', COALESCE((SELECT MAX(id) FROM usuarios), 1));

COMMIT;

