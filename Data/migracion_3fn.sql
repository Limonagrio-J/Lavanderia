-- =====================================================================
-- migracion_3fn.sql - Normalizacion 1FN -> 3FN del modulo de ventas
-- BD: Pepokita (PostgreSQL 18). Aplicar sobre la BD ya restaurada.
-- Operaciones destructivas: DROP COLUMN ventas.cliente,
--                           DROP COLUMN detalle_ventas.nombre_producto,
--                           DELETE de productos duplicados.
-- =====================================================================
BEGIN;

DO $guard$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND tablename = 'ventas')
       OR NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND tablename = 'detalle_ventas')
       OR NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND tablename = 'clientes')
       OR NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND tablename = 'productos') THEN
        RAISE EXCEPTION 'Faltan tablas base: restaura el backup antes de migrar.';
    END IF;
END
$guard$;

-- Cliente tecnico para ventas sin cliente identificado (evita id_cliente = 0).
INSERT INTO clientes (documento, nombre, apellido, telefono, tipo_cliente, fecha_registro)
VALUES ('00000000-CF', 'Consumidor', 'Final', '00000000', 'Particular', CURRENT_DATE)
ON CONFLICT (documento) DO NOTHING;

-- ---------------------------------------------------------------------
-- 1) ventas: texto redundante -> FK real a clientes
-- ---------------------------------------------------------------------
ALTER TABLE ventas ADD COLUMN IF NOT EXISTS id_cliente INTEGER;

UPDATE ventas v
   SET id_cliente = c.id
  FROM clientes c
 WHERE v.id_cliente IS NULL
   AND lower(btrim(c.nombre || ' ' || c.apellido)) = lower(btrim(v.cliente));

UPDATE ventas v
   SET id_cliente = (SELECT id FROM clientes WHERE documento = '00000000-CF')
 WHERE v.id_cliente IS NULL;

ALTER TABLE ventas ALTER COLUMN id_cliente SET NOT NULL;
ALTER TABLE ventas DROP COLUMN IF EXISTS cliente;

ALTER TABLE ventas DROP CONSTRAINT IF EXISTS ventas_id_cliente_fkey;
ALTER TABLE ventas ADD CONSTRAINT ventas_id_cliente_fkey
    FOREIGN KEY (id_cliente) REFERENCES clientes(id) ON DELETE RESTRICT;

ALTER TABLE ventas DROP CONSTRAINT IF EXISTS ventas_total_check;
ALTER TABLE ventas ADD CONSTRAINT ventas_total_check CHECK (total >= 0);

-- ---------------------------------------------------------------------
-- 2) detalle_ventas: snapshot de nombre -> FK real a productos
-- ---------------------------------------------------------------------
ALTER TABLE detalle_ventas ADD COLUMN IF NOT EXISTS id_producto INTEGER;

INSERT INTO productos (id, nombre, precio, activo)
VALUES (900, 'Producto historico', 0.00, FALSE)
ON CONFLICT (id) DO NOTHING;

UPDATE detalle_ventas dv
   SET id_producto = p.id
  FROM productos p
 WHERE lower(btrim(p.nombre)) = lower(btrim(dv.nombre_producto));

UPDATE detalle_ventas dv
   SET id_producto = 900
 WHERE dv.id_producto IS NULL
    OR dv.nombre_producto IS NULL
    OR NOT EXISTS (SELECT 1 FROM productos p WHERE p.id = dv.id_producto);

ALTER TABLE detalle_ventas ALTER COLUMN id_producto SET NOT NULL;
ALTER TABLE detalle_ventas DROP COLUMN IF EXISTS nombre_producto;

ALTER TABLE detalle_ventas DROP CONSTRAINT IF EXISTS detalle_ventas_id_producto_fkey;
ALTER TABLE detalle_ventas ADD CONSTRAINT detalle_ventas_id_producto_fkey
    FOREIGN KEY (id_producto) REFERENCES productos(id) ON DELETE RESTRICT;

UPDATE detalle_ventas
   SET subtotal = ROUND(cantidad * precio_unitario, 2);

ALTER TABLE detalle_ventas DROP CONSTRAINT IF EXISTS detalle_ventas_subtotal_check;
ALTER TABLE detalle_ventas ADD CONSTRAINT detalle_ventas_subtotal_check
    CHECK (subtotal = ROUND(cantidad * precio_unitario, 2));

UPDATE ventas v
   SET total = COALESCE((SELECT SUM(d.subtotal) FROM detalle_ventas d WHERE d.id_venta = v.id), 0)
 WHERE EXISTS (SELECT 1 FROM detalle_ventas d WHERE d.id_venta = v.id);

-- ---------------------------------------------------------------------
-- 3) Unicidad de catalogo y limpieza de duplicados (requerido por el seed)
-- ---------------------------------------------------------------------
UPDATE detalle_ventas dv
   SET id_producto = q.id
  FROM productos p, productos q
 WHERE dv.id_producto = p.id
   AND p.id > q.id
   AND lower(btrim(p.nombre)) = lower(btrim(q.nombre));

DELETE FROM productos p
 USING productos q
 WHERE p.id > q.id
   AND lower(btrim(p.nombre)) = lower(btrim(q.nombre));

ALTER TABLE productos DROP CONSTRAINT IF EXISTS productos_nombre_key;
ALTER TABLE productos ADD CONSTRAINT productos_nombre_key UNIQUE (nombre);

-- ---------------------------------------------------------------------
-- 4) Indices de apoyo a las nuevas FKs y a los filtros de la interfaz
-- ---------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_ventas_cliente ON ventas (id_cliente);
CREATE INDEX IF NOT EXISTS idx_ventas_fecha ON ventas (fecha DESC);
CREATE INDEX IF NOT EXISTS idx_detalle_ventas_venta ON detalle_ventas (id_venta);
CREATE INDEX IF NOT EXISTS idx_detalle_ventas_producto ON detalle_ventas (id_producto);
CREATE INDEX IF NOT EXISTS idx_orden_detalles_servicio ON orden_detalles (servicio_id);
CREATE INDEX IF NOT EXISTS idx_precios_servicio_servicio ON precios_servicio (servicio_id);
CREATE INDEX IF NOT EXISTS idx_precios_servicio_tipo ON precios_servicio (tipo_vehiculo_id);
CREATE INDEX IF NOT EXISTS idx_empleados_rol ON empleados (rol_id);

-- ---------------------------------------------------------------------
-- 5) Restricciones de dominio complementarias (3FN / integridad)
-- ---------------------------------------------------------------------
ALTER TABLE pagos DROP CONSTRAINT IF EXISTS pagos_metodo_pago_check;
ALTER TABLE pagos ADD CONSTRAINT pagos_metodo_pago_check
    CHECK (metodo_pago IN ('Efectivo', 'Tarjeta', 'Transferencia', 'Yape', 'Plin', 'Cheque'));

ALTER TABLE detalle_ventas DROP CONSTRAINT IF EXISTS detalle_ventas_precio_unitario_check;
ALTER TABLE detalle_ventas ADD CONSTRAINT detalle_ventas_precio_unitario_check
    CHECK (precio_unitario >= 0);

ALTER TABLE productos DROP CONSTRAINT IF EXISTS productos_precio_check;
ALTER TABLE productos ADD CONSTRAINT productos_precio_check CHECK (precio >= 0);

ALTER TABLE precios_servicio DROP CONSTRAINT IF EXISTS precios_servicio_precio_check;
ALTER TABLE precios_servicio ADD CONSTRAINT precios_servicio_precio_check CHECK (precio >= 0);

ALTER TABLE orden_detalles DROP CONSTRAINT IF EXISTS orden_detalles_precio_cobrado_check;
ALTER TABLE orden_detalles ADD CONSTRAINT orden_detalles_precio_cobrado_check
    CHECK (precio_cobrado >= 0);

ALTER TABLE pagos DROP CONSTRAINT IF EXISTS pagos_monto_pagado_check;
ALTER TABLE pagos ADD CONSTRAINT pagos_monto_pagado_check CHECK (monto_pagado > 0);

ALTER TABLE servicios DROP CONSTRAINT IF EXISTS servicios_duracion_estimada_min_check;
ALTER TABLE servicios ADD CONSTRAINT servicios_duracion_estimada_min_check
    CHECK (duracion_estimada_min > 0);

COMMIT;

-- =====================================================================
-- CR-01: Control de Descuentos y Trazabilidad (idempotente)
-- Usuarios/login, rol Vendedor, descuento 0-20% y trazabilidad en ventas.
-- =====================================================================

-- 1) Tabla usuarios (1:1 con empleados) y completitud de columnas
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

-- 3) Nuevas columnas en ventas
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
 WHERE subtotal_bruto IS NULL OR total_final IS NULL OR vendedor IS NULL OR BTRIM(vendedor) = '';

ALTER TABLE ventas ALTER COLUMN subtotal_bruto SET NOT NULL;
ALTER TABLE ventas ALTER COLUMN total_final   SET NOT NULL;

-- 5) Restricciones de negocio y coherencia
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

-- 7) Usuario inicial (vendedor) - contrasena: Vendedor123! (PBKDF2 v1)
INSERT INTO empleados (rol_id, documento, nombre, apellido, telefono, activo)
SELECT r.id, 'VEN-0001', 'Carlos', 'Perez', '70000001', TRUE
  FROM roles r WHERE r.nombre = 'Vendedor'
ON CONFLICT (documento) DO NOTHING;

INSERT INTO usuarios (empleado_id, nombre_usuario, contrasena_hash, activo, intentos_fallidos, bloqueado)
SELECT e.id, 'cperez', 'v1$JU6Ff5stfcumJuRafzaB2Q==$pOnR6xCWUYsaLgtoDEONPxrl1xxRZVcTKB6Aa6V87Lg=', TRUE, 0, FALSE
  FROM empleados e WHERE e.documento = 'VEN-0001'
ON CONFLICT (nombre_usuario) DO NOTHING;
