using Npgsql;
using NpgsqlTypes;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Data
{
    public class VentaDAO
    {
        private readonly string _cadenaConexion;

        public VentaDAO(IConfiguration configuracion)
        {
            _cadenaConexion = configuracion.GetConnectionString("Pepokita")
                ?? throw new InvalidOperationException("Falta la cadena de conexión 'Pepokita'.");
        }

        public void AsegurarEstructura()
        {
            const string sql = @"
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

                -- CR-01: usuarios / rol / descuentos y trazabilidad
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

                INSERT INTO empleados (rol_id, documento, nombre, apellido, telefono, activo)
                SELECT r.id, 'VEN-0001', 'Carlos', 'Perez', '70000001', TRUE
                  FROM roles r WHERE r.nombre = 'Vendedor'
                ON CONFLICT (documento) DO NOTHING;

                INSERT INTO usuarios (empleado_id, nombre_usuario, contrasena_hash, activo, intentos_fallidos, bloqueado)
                SELECT e.id, 'cperez', 'v1$JU6Ff5stfcumJuRafzaB2Q==$pOnR6xCWUYsaLgtoDEONPxrl1xxRZVcTKB6Aa6V87Lg=', TRUE, 0, FALSE
                  FROM empleados e WHERE e.documento = 'VEN-0001'
                ON CONFLICT (nombre_usuario) DO NOTHING;
            ";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            comando.ExecuteNonQuery();
        }

        public int ObtenerIdConsumidorFinal()
        {
            const string documento = "00000000-CF";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();

            using (var insercion = new NpgsqlCommand(
                @"INSERT INTO clientes (documento, nombre, apellido, telefono, tipo_cliente)
                  VALUES (@documento, 'Consumidor', 'Final', '00000000', 'Particular')
                  ON CONFLICT (documento) DO NOTHING", conexion))
            {
                insercion.Parameters.AddWithValue("documento", documento);
                insercion.ExecuteNonQuery();
            }

            using var consulta = new NpgsqlCommand(
                "SELECT id FROM clientes WHERE documento = @documento", conexion);
            consulta.Parameters.AddWithValue("documento", documento);
            return Convert.ToInt32(consulta.ExecuteScalar());
        }

        public List<Producto> ObtenerProductos()
        {
            var lista = new List<Producto>();
            const string sql = "SELECT id, nombre, precio, activo FROM productos WHERE activo = TRUE ORDER BY nombre";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            using var lector = comando.ExecuteReader();
            while (lector.Read())
            {
                lista.Add(new Producto
                {
                    Id = lector.GetInt32(0),
                    Nombre = lector.GetString(1),
                    Precio = lector.GetDecimal(2),
                    Activo = lector.GetBoolean(3)
                });
            }
            return lista;
        }

        public int RegistrarVenta(Venta venta)
        {
            int idCliente = venta.IdCliente != 0 ? venta.IdCliente : ObtenerIdConsumidorFinal();

            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var transaccion = conexion.BeginTransaction();

            try
            {
                const string sqlCabecera = @"
                    INSERT INTO ventas (fecha, id_cliente, total, vendedor, id_vendedor,
                                        subtotal_bruto, porcentaje_descuento, monto_descuento, total_final)
                    VALUES (@fecha, @idCliente, @total, @vendedor, @idVendedor,
                            @subtotalBruto, @porcentajeDescuento, @montoDescuento, @totalFinal)
                    RETURNING id";
                int idVentaGenerado;

                using (var cmdCab = new NpgsqlCommand(sqlCabecera, conexion, transaccion))
                {
                    cmdCab.Parameters.Add(new NpgsqlParameter("fecha", NpgsqlDbType.Timestamp)
                    {
                        Value = DateTime.SpecifyKind(venta.Fecha, DateTimeKind.Unspecified)
                    });
                    cmdCab.Parameters.AddWithValue("idCliente", idCliente);
                    cmdCab.Parameters.AddWithValue("total", venta.TotalFinal);
                    cmdCab.Parameters.AddWithValue("vendedor", venta.Vendedor ?? string.Empty);
                    cmdCab.Parameters.AddWithValue("idVendedor", (object?)venta.IdVendedor ?? DBNull.Value);
                    cmdCab.Parameters.AddWithValue("subtotalBruto", venta.SubtotalBruto);
                    cmdCab.Parameters.AddWithValue("porcentajeDescuento", venta.PorcentajeDescuento);
                    cmdCab.Parameters.AddWithValue("montoDescuento", venta.MontoDescuento);
                    cmdCab.Parameters.AddWithValue("totalFinal", venta.TotalFinal);
                    idVentaGenerado = Convert.ToInt32(cmdCab.ExecuteScalar());
                }

                const string sqlDetalle = @"
                    INSERT INTO detalle_ventas (id_venta, id_producto, cantidad, precio_unitario, subtotal)
                    VALUES (@idVenta, @idProd, @cant, @precio, @sub)";

                foreach (var item in venta.Detalles)
                {
                    using var cmdDet = new NpgsqlCommand(sqlDetalle, conexion, transaccion);
                    cmdDet.Parameters.AddWithValue("idVenta", idVentaGenerado);
                    cmdDet.Parameters.AddWithValue("idProd", item.IdProducto);
                    cmdDet.Parameters.AddWithValue("cant", item.Cantidad);
                    cmdDet.Parameters.AddWithValue("precio", item.PrecioUnitario);
                    cmdDet.Parameters.AddWithValue("sub", item.Subtotal);
                    cmdDet.ExecuteNonQuery();
                }

                transaccion.Commit();
                return idVentaGenerado;
            }
            catch (Exception)
            {
                transaccion.Rollback();
                throw;
            }
        }

        public List<VentaHistorial> ObtenerHistorial()
        {
            var lista = new List<VentaHistorial>();
            const string sql = @"
                SELECT v.id, v.fecha,
                       COALESCE(c.nombre || ' ' || c.apellido, 'Consumidor Final') AS cliente,
                       COALESCE(v.vendedor, 'Sin registrar') AS vendedor,
                       COALESCE(v.subtotal_bruto, v.total) AS subtotal_bruto,
                       COALESCE(v.porcentaje_descuento, 0) AS porcentaje_descuento,
                       COALESCE(v.monto_descuento, 0) AS monto_descuento,
                       COALESCE(v.total_final, v.total) AS total_final
                  FROM ventas v
                  LEFT JOIN clientes c ON c.id = v.id_cliente
                 ORDER BY v.fecha DESC, v.id DESC";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            using var lector = comando.ExecuteReader();
            while (lector.Read())
            {
                lista.Add(new VentaHistorial
                {
                    Id = lector.GetInt32(0),
                    Fecha = lector.GetDateTime(1),
                    Cliente = lector.GetString(2),
                    Vendedor = lector.GetString(3),
                    SubtotalBruto = lector.GetDecimal(4),
                    PorcentajeDescuento = lector.GetDecimal(5),
                    MontoDescuento = lector.GetDecimal(6),
                    TotalFinal = lector.GetDecimal(7)
                });
            }
            return lista;
        }

        public List<ResumenDescuentoVendedor> ObtenerAuditoriaDescuentos()
        {
            var lista = new List<ResumenDescuentoVendedor>();
            const string sql = @"
                SELECT vendedor,
                       COUNT(*) AS ventas_con_descuento,
                       COALESCE(SUM(monto_descuento), 0) AS total_descontado
                  FROM ventas
                 WHERE porcentaje_descuento > 0
                 GROUP BY vendedor
                 ORDER BY total_descontado DESC";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            using var lector = comando.ExecuteReader();
            while (lector.Read())
            {
                lista.Add(new ResumenDescuentoVendedor
                {
                    Vendedor = lector.GetString(0),
                    VentasConDescuento = Convert.ToInt32(lector.GetInt64(1)),
                    TotalDescontado = lector.GetDecimal(2)
                });
            }
            return lista;
        }
    }
}
