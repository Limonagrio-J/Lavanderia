-- =====================================================================
-- seed_autolavado.sql - Dominio Autolavado + datos de prueba
-- Idempotente: INSERT ... ON CONFLICT DO NOTHING.
-- Requiere aplicar primero migracion_3fn.sql.
-- Operaciones destructivas: UPDATE de nombres de servicios,
--                           DELETE de productos del catalogo legacy.
-- =====================================================================
BEGIN;

-- Roles y tipos de vehiculo
INSERT INTO roles (nombre) VALUES
    ('Administrador'), ('Recepcionista'), ('Operario')
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO tipos_vehiculo (nombre) VALUES
    ('Automovil'), ('Motocicleta'), ('SUV'), ('Pickup')
ON CONFLICT (nombre) DO NOTHING;

INSERT INTO empleados (rol_id, documento, nombre, apellido, telefono, activo)
SELECT r.id, '1234567', 'Juan', 'Perez', '70000000', TRUE
FROM roles r WHERE r.nombre = 'Operario'
ON CONFLICT (documento) DO NOTHING;

-- Servicios: estandarizar los existentes y agregar el resto del catalogo
DO $serv$
BEGIN
    IF EXISTS (SELECT 1 FROM servicios WHERE lower(btrim(nombre)) = 'lavado exterior')
       AND NOT EXISTS (SELECT 1 FROM servicios WHERE nombre = 'Lavado Exterior') THEN
        UPDATE servicios
           SET nombre = 'Lavado Exterior',
               descripcion = 'Lavado de carroceria y llantas',
               duracion_estimada_min = 20
         WHERE lower(btrim(nombre)) = 'lavado exterior';
    END IF;

    IF EXISTS (SELECT 1 FROM servicios WHERE lower(btrim(nombre)) = 'lavado completo')
       AND NOT EXISTS (SELECT 1 FROM servicios WHERE nombre = 'Lavado Completo') THEN
        UPDATE servicios
           SET nombre = 'Lavado Completo',
               descripcion = 'Exterior mas limpieza interior',
               duracion_estimada_min = 45
         WHERE lower(btrim(nombre)) = 'lavado completo';
    END IF;
END
$serv$;

INSERT INTO servicios (nombre, descripcion, duracion_estimada_min, activo) VALUES
    ('Encerado y Pulido', 'Cera y pulido de carroceria', 60, TRUE),
    ('Limpieza de Tapiz', 'Limpieza profunda de asientos y alfombras', 75, TRUE),
    ('Shampoo de Motor', 'Lavado y desengrasado del compartimiento del motor', 40, TRUE),
    ('Aromatizacion', 'Aromatizacion interior del vehiculo', 10, TRUE),
    ('Aspirado Interior', 'Aspirado de asientos, alfombras y tablero', 25, TRUE),
    ('Sanitizado Interior', 'Desinfeccion de superficies interiores', 30, TRUE)
ON CONFLICT (nombre) DO NOTHING;

-- Catalogo de productos (ventas rapidas)
INSERT INTO productos (nombre, precio, activo) VALUES
    ('Lavado Exterior', 30.00, TRUE),
    ('Lavado Completo', 60.00, TRUE),
    ('Encerado y Pulido', 90.00, TRUE),
    ('Limpieza de Tapiz', 75.00, TRUE),
    ('Shampoo de Motor', 50.00, TRUE),
    ('Aromatizacion', 10.00, TRUE),
    ('Aspirado Interior', 20.00, TRUE),
    ('Sanitizado Interior', 35.00, TRUE)
ON CONFLICT (nombre) DO NOTHING;

-- Reasignar cualquier venta historica que apuntara al catalogo legacy
UPDATE detalle_ventas dv
   SET id_producto = p.id
  FROM productos old, productos p
 WHERE dv.id_producto = old.id
   AND (
        lower(btrim(old.nombre)) LIKE '%kilo%'
     OR lower(btrim(old.nombre)) LIKE '%planchad%'
     OR lower(btrim(old.nombre)) LIKE '%seco%'
     OR lower(btrim(old.nombre)) LIKE '%detergente%'
     OR lower(btrim(old.nombre)) LIKE '%suavizante%'
     OR lower(btrim(old.nombre)) LIKE '%lavand%'
   )
   AND p.nombre = CASE
        WHEN lower(btrim(old.nombre)) LIKE '%kilo%' THEN 'Lavado Exterior'
        WHEN lower(btrim(old.nombre)) LIKE '%planchad%' THEN 'Lavado Completo'
        WHEN lower(btrim(old.nombre)) LIKE '%seco%' THEN 'Limpieza de Tapiz'
        WHEN lower(btrim(old.nombre)) LIKE '%detergente%' THEN 'Shampoo de Motor'
        ELSE 'Aromatizacion'
   END;

DELETE FROM productos
 WHERE lower(btrim(nombre)) LIKE '%kilo%'
    OR lower(btrim(nombre)) LIKE '%planchad%'
    OR lower(btrim(nombre)) LIKE '%seco%'
    OR lower(btrim(nombre)) LIKE '%detergente%'
    OR lower(btrim(nombre)) LIKE '%suavizante%'
    OR lower(btrim(nombre)) LIKE '%lavand%';

-- Tarifas por servicio y tipo de vehiculo
INSERT INTO precios_servicio (servicio_id, tipo_vehiculo_id, precio)
SELECT s.id, tv.id,
       ROUND(
         CASE lower(btrim(s.nombre))
             WHEN 'lavado exterior' THEN 30
             WHEN 'lavado completo' THEN 60
             WHEN 'encerado y pulido' THEN 90
             WHEN 'limpieza de tapiz' THEN 75
             WHEN 'shampoo de motor' THEN 50
             WHEN 'aromatizacion' THEN 10
             WHEN 'aspirado interior' THEN 20
             WHEN 'sanitizado interior' THEN 35
             ELSE 30
         END
         * CASE lower(btrim(tv.nombre))
             WHEN 'motocicleta' THEN 0.70
             WHEN 'suv' THEN 1.25
             WHEN 'pickup' THEN 1.20
             ELSE 1.00
           END
       , 2)
  FROM servicios s
 CROSS JOIN tipos_vehiculo tv
ON CONFLICT (servicio_id, tipo_vehiculo_id) DO NOTHING;

-- 20 clientes
INSERT INTO clientes (documento, nombre, apellido, telefono, correo, direccion, tipo_cliente, fecha_registro) VALUES
 ('10000001', 'Carlos', 'Mendoza', '71234567', 'carlos.mendoza@example.com', 'Av. Banzer #123', 'Frecuente', '2026-09-03'),
 ('10000002', 'Maria', 'Quispe', '72345678', 'maria.quispe@example.com', 'Calle Sucre #45', 'Particular', '2026-09-11'),
 ('10000003', 'Luis Fernando', 'Arce', '73456789', 'luis.arce@example.com', 'Av. Cristo Redentor km 3', 'Empresa', '2026-09-18'),
 ('10000004', 'Andrea', 'Villarroel', '74567890', 'andrea.v@example.com', 'Calle Ballivian #78', 'Particular', '2026-09-25'),
 ('10000005', 'Javier', 'Zambrana', '75678901', 'javier.z@example.com', 'Zona Sur, calle 12', 'Frecuente', '2026-10-02'),
 ('10000006', 'Patricia', 'Cortez', '76789012', 'patricia.c@example.com', 'Av. Guayaramerin #9', 'Particular', '2026-10-06'),
 ('10000007', 'Diego', 'Suarez', '77890123', 'diego.s@example.com', 'Calle Comercio #301', 'Empresa', '2026-10-09'),
 ('10000008', 'Gabriela', 'Rojas', '78901234', 'gabriela.r@example.com', 'Zona Norte, calle 7', 'Particular', '2026-10-13'),
 ('10000009', 'Ramiro', 'Flores', '79012345', 'ramiro.f@example.com', 'Av.Piece #218', 'Frecuente', '2026-10-17'),
 ('10000010', 'Claudia', 'Paredes', '70123456', 'claudia.p@example.com', 'Calle Murillo #56', 'Particular', '2026-10-21'),
 ('10000011', 'Miguel Angel', 'Navarro', '71122334', 'miguel.n@example.com', 'Av. Manco Capac #88', 'Empresa', '2026-10-24'),
 ('10000012', 'Sandra', 'Heredia', '72233445', 'sandra.h@example.com', 'Calle Avaroa #14', 'Particular', '2026-10-28'),
 ('10000013', 'Andres', 'Condori', '73344556', 'andres.c@example.com', 'Zona Oeste, calle 3', 'Frecuente', '2026-11-01'),
 ('10000014', 'Lorena', 'Aguirre', '74455667', 'lorena.a@example.com', 'Av. Cristo Redentor km 5', 'Particular', '2026-11-03'),
 ('10000015', 'Fernando', 'Iriarte', '75566778', 'fernando.i@example.com', 'Calle Junin #92', 'Particular', '2026-11-05'),
 ('10000016', 'Veronica', 'Salazar', '76677889', 'veronica.s@example.com', 'Av. El Sillar #33', 'Empresa', '2026-11-07'),
 ('10000017', 'Rodrigo', 'Vargas', '77788990', 'rodrigo.v@example.com', 'Calle Bolivia #115', 'Frecuente', '2026-11-09'),
 ('10000018', 'Natalia', 'Monje', '78899001', 'natalia.m@example.com', 'Zona Sur, calle 21', 'Particular', '2026-11-11'),
 ('10000019', 'Sergio', 'Paz', '79900112', 'sergio.p@example.com', 'Av. Banzer #540', 'Particular', '2026-11-13'),
 ('10000020', 'Elizabeth', 'Cespedes', '70011223', 'elizabeth.c@example.com', 'Calle Cochabamba #67', 'Frecuente', '2026-11-15')
ON CONFLICT (documento) DO NOTHING;

-- 20 vehiculos vinculados a sus clientes
INSERT INTO vehiculos (cliente_id, tipo_vehiculo_id, placa, marca, modelo, color, activo)
SELECT c.id, tv.id, v.placa, v.marca, v.modelo, v.color, TRUE
  FROM (VALUES
    ('10000001', 'Automovil',   '1001-ABC', 'Toyota',     'Corolla 1.6',   'Blanco'),
    ('10000002', 'Automovil',   '1002-ABC', 'Honda',      'Civic 1.8',     'Gris'),
    ('10000003', 'SUV',         '1003-ABC', 'Toyota',     'RAV4 2.0',      'Negro'),
    ('10000004', 'Automovil',   '1004-ABC', 'Chevrolet',  'Onix 1.4',      'Rojo'),
    ('10000005', 'Motocicleta', '1005-ABC', 'Yamaha',     'FZ 150',        'Negro'),
    ('10000006', 'Automovil',   '1006-ABC', 'Ford',       'Fiesta 1.6',    'Azul'),
    ('10000007', 'Pickup',      '1007-ABC', 'Toyota',     'Hilux 2.8',     'Blanco'),
    ('10000008', 'Automovil',   '1008-ABC', 'Volkswagen', 'Gol 1.6',       'Plata'),
    ('10000009', 'SUV',         '1009-ABC', 'Honda',      'CR-V 2.4',      'Gris'),
    ('10000010', 'Automovil',   '1010-ABC', 'Renault',    'Clio 1.2',      'Blanco'),
    ('10000011', 'Automovil',   '1011-ABC', 'Kia',        'Rio 1.4',       'Negro'),
    ('10000012', 'Motocicleta', '1012-ABC', 'Honda',      'CG 150',        'Rojo'),
    ('10000013', 'Automovil',   '1013-ABC', 'Hyundai',    'Atos 1.0',      'Azul'),
    ('10000014', 'SUV',         '1014-ABC', 'Mazda',      'CX-5 2.0',      'Plata'),
    ('10000015', 'Pickup',      '1015-ABC', 'Ford',       'Ranger 2.2',    'Blanco'),
    ('10000016', 'Automovil',   '1016-ABC', 'Chevrolet',  'Sprint 1.4',    'Gris'),
    ('10000017', 'Motocicleta', '1017-ABC', 'Suzuki',     'DR 150',        'Negro'),
    ('10000018', 'Automovil',   '1018-ABC', 'Nissan',     'Sunny 1.6',     'Blanco'),
    ('10000019', 'SUV',         '1019-ABC', 'Nissan',     'X-Trail 2.0',   'Azul'),
    ('10000020', 'Motocicleta', '1020-ABC', 'Bajaj',      'Discover 150',  'Rojo')
) AS v(doc, tipo, placa, marca, modelo, color)
  JOIN clientes c ON c.documento = v.doc
  JOIN tipos_vehiculo tv ON lower(btrim(tv.nombre)) = lower(v.tipo)
ON CONFLICT (placa) DO NOTHING;

-- 8 ventas historicas con FK real a clientes y productos
WITH v AS (
    INSERT INTO ventas (id, fecha, id_cliente, total) VALUES
        (9001, TIMESTAMP '2026-11-02 09:15', (SELECT id FROM clientes WHERE documento = '10000001'), 50.00),
        (9002, TIMESTAMP '2026-11-03 11:40', (SELECT id FROM clientes WHERE documento = '10000002'), 80.00),
        (9003, TIMESTAMP '2026-11-05 16:05', (SELECT id FROM clientes WHERE documento = '10000003'), 90.00),
        (9004, TIMESTAMP '2026-11-07 10:20', (SELECT id FROM clientes WHERE documento = '10000004'), 105.00),
        (9005, TIMESTAMP '2026-11-09 15:35', (SELECT id FROM clientes WHERE documento = '10000005'), 110.00),
        (9006, TIMESTAMP '2026-11-11 08:50', (SELECT id FROM clientes WHERE documento = '10000006'), 80.00),
        (9007, TIMESTAMP '2026-11-13 17:10', (SELECT id FROM clientes WHERE documento = '10000007'), 60.00),
        (9008, TIMESTAMP '2026-11-16 12:25', (SELECT id FROM clientes WHERE documento = '10000008'), 160.00)
    ON CONFLICT (id) DO NOTHING
    RETURNING id
), d AS (
    SELECT * FROM (VALUES
        (9001, 'Lavado Exterior', 1),
        (9001, 'Aspirado Interior', 1),
        (9002, 'Lavado Completo', 1),
        (9002, 'Aromatizacion', 2),
        (9003, 'Encerado y Pulido', 1),
        (9004, 'Limpieza de Tapiz', 1),
        (9004, 'Lavado Exterior', 1),
        (9005, 'Shampoo de Motor', 1),
        (9005, 'Lavado Completo', 1),
        (9006, 'Sanitizado Interior', 2),
        (9006, 'Aromatizacion', 1),
        (9007, 'Lavado Exterior', 2),
        (9008, 'Lavado Completo', 1),
        (9008, 'Encerado y Pulido', 1),
        (9008, 'Aromatizacion', 1)
    ) AS x(id_venta, producto, cantidad)
)
INSERT INTO detalle_ventas (id, id_venta, id_producto, cantidad, precio_unitario, subtotal)
SELECT (ROW_NUMBER() OVER (ORDER BY d.id_venta, d.producto) + 90000)::int,
       d.id_venta, p.id, d.cantidad, p.precio, ROUND(p.precio * d.cantidad, 2)
  FROM d
  JOIN v ON v.id = d.id_venta
  JOIN productos p ON p.nombre = d.producto
ON CONFLICT (id) DO NOTHING;

-- 6 ordenes de servicio con estados del flujo de lavado
WITH ins AS (
    INSERT INTO ordenes_servicio
        (id, vehiculo_id, empleado_id, fecha_recepcion, estado, observaciones, total_pagar)
    SELECT o.id, veh.id, emp.id, o.fecha, o.estado::estado_orden_enum, o.obs, o.total
      FROM (VALUES
        (9001, '1001-ABC', TIMESTAMP '2026-11-10 08:00', 'pendiente',  'Revisar llantas delanteras', 30.00),
        (9002, '1002-ABC', TIMESTAMP '2026-11-11 09:30', 'en_proceso', NULL, 60.00),
        (9003, '1003-ABC', TIMESTAMP '2026-11-12 10:15', 'listo',      'Cliente avisado por telefono', 90.00),
        (9004, '1004-ABC', TIMESTAMP '2026-11-13 11:00', 'entregado',  NULL, 85.00),
        (9005, '1005-ABC', TIMESTAMP '2026-11-14 14:45', 'entregado',  'Moto con olor a combustible', 50.00),
        (9006, '1006-ABC', TIMESTAMP '2026-11-15 16:20', 'cancelado',  'Cliente no llego', 20.00)
    ) AS o(id, placa, fecha, estado, obs, total)
      JOIN vehiculos veh ON veh.placa = o.placa
      JOIN empleados emp ON emp.documento = '1234567'
    ON CONFLICT (id) DO NOTHING
    RETURNING id
), det AS (
    SELECT * FROM (VALUES
        (9001, 'Lavado Exterior', 30.00),
        (9002, 'Lavado Completo', 60.00),
        (9003, 'Encerado y Pulido', 90.00),
        (9004, 'Limpieza de Tapiz', 75.00),
        (9004, 'Aromatizacion', 10.00),
        (9005, 'Shampoo de Motor', 50.00),
        (9006, 'Aspirado Interior', 20.00)
    ) AS x(id_orden, servicio, precio)
)
INSERT INTO orden_detalles (orden_id, servicio_id, precio_cobrado)
SELECT ins.id, s.id, det.precio
  FROM det
  JOIN ins ON ins.id = det.id_orden
  JOIN servicios s ON s.nombre = det.servicio;

INSERT INTO pagos (orden_id, monto_pagado, metodo_pago, fecha_pago)
SELECT o.id, o.total_pagar, 'Tarjeta', TIMESTAMP '2026-11-16 09:00'
  FROM ordenes_servicio o
 WHERE o.id BETWEEN 9001 AND 9006
   AND o.estado = 'entregado'::estado_orden_enum
ON CONFLICT (orden_id) DO NOTHING;

SELECT setval('roles_id_seq', COALESCE((SELECT MAX(id) FROM roles), 1));
SELECT setval('tipos_vehiculo_id_seq', COALESCE((SELECT MAX(id) FROM tipos_vehiculo), 1));
SELECT setval('empleados_id_seq', COALESCE((SELECT MAX(id) FROM empleados), 1));
SELECT setval('servicios_id_seq', COALESCE((SELECT MAX(id) FROM servicios), 1));
SELECT setval('clientes_id_seq', COALESCE((SELECT MAX(id) FROM clientes), 1));
SELECT setval('vehiculos_id_seq', COALESCE((SELECT MAX(id) FROM vehiculos), 1));
SELECT setval('productos_id_seq', COALESCE((SELECT MAX(id) FROM productos), 1));
SELECT setval('precios_servicio_id_seq', COALESCE((SELECT MAX(id) FROM precios_servicio), 1));
SELECT setval('ventas_id_seq', COALESCE((SELECT MAX(id) FROM ventas), 1));
SELECT setval('detalle_ventas_id_seq', COALESCE((SELECT MAX(id) FROM detalle_ventas), 1));
SELECT setval('ordenes_servicio_id_seq', COALESCE((SELECT MAX(id) FROM ordenes_servicio), 1));
SELECT setval('orden_detalles_id_seq', COALESCE((SELECT MAX(id) FROM orden_detalles), 1));
SELECT setval('pagos_id_seq', COALESCE((SELECT MAX(id) FROM pagos), 1));

COMMIT;
