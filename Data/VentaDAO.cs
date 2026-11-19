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

        // Crea las tablas (si no existen) y carga productos de ejemplo la primera vez
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
                WHERE NOT EXISTS (SELECT 1 FROM productos);";
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

        // Clientes que tienen al menos una compra (incluye los dados de baja, para no perder su historial)
        public List<Cliente> ObtenerClientesConCompras()
        {
            var lista = new List<Cliente>();
            const string sql = @"SELECT DISTINCT c.id, c.nombre, c.apellido, c.documento
                                 FROM clientes c
                                 JOIN ventas v ON v.id_cliente = c.id
                                 ORDER BY c.apellido, c.nombre";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            using var lector = comando.ExecuteReader();
            while (lector.Read())
            {
                lista.Add(new Cliente
                {
                    Id = lector.GetInt32(0),
                    Nombre = lector.GetString(1),
                    Apellido = lector.GetString(2),
                    Documento = lector.GetString(3)
                });
            }
            return lista;
        }

        // Historial de compras: filtra por rango de fechas (inclusive) y/o cliente (0 = todos)
        public List<VentaHistorial> ObtenerHistorial(DateOnly? desde, DateOnly? hasta, int idCliente)
        {
            var ventas = new List<VentaHistorial>();

            var sql = @"SELECT v.id, v.fecha, v.id_cliente, c.nombre, c.apellido, c.documento, v.total
                        FROM ventas v
                        JOIN clientes c ON c.id = v.id_cliente
                        WHERE 1 = 1";
            if (desde.HasValue) sql += " AND v.fecha >= @desde";
            if (hasta.HasValue) sql += " AND v.fecha < @hasta";
            if (idCliente > 0) sql += " AND v.id_cliente = @idCliente";
            sql += " ORDER BY v.fecha DESC, v.id DESC";

            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();

            using (var comando = new NpgsqlCommand(sql, conexion))
            {
                if (desde.HasValue)
                {
                    comando.Parameters.Add(new NpgsqlParameter("desde", NpgsqlDbType.Timestamp)
                    {
                        Value = desde.Value.ToDateTime(TimeOnly.MinValue)
                    });
                }
                if (hasta.HasValue)
                {
                    // "hasta" es inclusivo: se compara contra el inicio del día siguiente
                    comando.Parameters.Add(new NpgsqlParameter("hasta", NpgsqlDbType.Timestamp)
                    {
                        Value = hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)
                    });
                }
                if (idCliente > 0) comando.Parameters.AddWithValue("idCliente", idCliente);

                using var lector = comando.ExecuteReader();
                while (lector.Read())
                {
                    ventas.Add(new VentaHistorial
                    {
                        Id = lector.GetInt32(0),
                        Fecha = lector.GetDateTime(1),
                        IdCliente = lector.GetInt32(2),
                        Cliente = $"{lector.GetString(3)} {lector.GetString(4)}".Trim(),
                        Documento = lector.GetString(5),
                        Total = lector.GetDecimal(6)
                    });
                }
            }

            if (ventas.Count == 0) return ventas;

            // Líneas de todas las ventas encontradas, en una sola consulta
            const string sqlLineas = @"SELECT d.id_venta, p.nombre, d.cantidad, d.precio_unitario, d.subtotal
                                       FROM detalle_ventas d
                                       JOIN productos p ON p.id = d.id_producto
                                       WHERE d.id_venta = ANY(@ids)
                                       ORDER BY d.id";
            var porId = ventas.ToDictionary(v => v.Id);
            using (var cmdLineas = new NpgsqlCommand(sqlLineas, conexion))
            {
                cmdLineas.Parameters.AddWithValue("ids", ventas.Select(v => v.Id).ToArray());
                using var lector = cmdLineas.ExecuteReader();
                while (lector.Read())
                {
                    if (porId.TryGetValue(lector.GetInt32(0), out var venta))
                    {
                        venta.Lineas.Add(new LineaHistorial
                        {
                            Producto = lector.GetString(1),
                            Cantidad = lector.GetInt32(2),
                            PrecioUnitario = lector.GetDecimal(3),
                            Subtotal = lector.GetDecimal(4)
                        });
                    }
                }
            }

            return ventas;
        }

        // Maestro-Detalle ATÓMICO: o se guarda todo, o no se guarda nada
        public int RegistrarVenta(Venta venta)
        {
            int idCliente = venta.IdCliente != 0 ? venta.IdCliente : ObtenerIdConsumidorFinal();

            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var transaccion = conexion.BeginTransaction();

            try
            {
                // 1. Cabecera (RETURNING id devuelve el número de venta generado)
                const string sqlCabecera =
                    "INSERT INTO ventas (fecha, id_cliente, total) VALUES (@fecha, @idCliente, @total) RETURNING id";
                int idVentaGenerado;

                using (var cmdCab = new NpgsqlCommand(sqlCabecera, conexion, transaccion))
                {
                    cmdCab.Parameters.Add(new NpgsqlParameter("fecha", NpgsqlDbType.Timestamp)
                    {
                        Value = DateTime.SpecifyKind(venta.Fecha, DateTimeKind.Unspecified)
                    });
                    cmdCab.Parameters.AddWithValue("idCliente", idCliente);
                    cmdCab.Parameters.AddWithValue("total", venta.Total);
                    idVentaGenerado = Convert.ToInt32(cmdCab.ExecuteScalar());
                }

                // 2. Cada línea del detalle, ligada al id de la cabecera
                const string sqlDetalle = @"INSERT INTO detalle_ventas
                    (id_venta, id_producto, cantidad, precio_unitario, subtotal)
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

                // 3. Todo salió bien: confirmamos
                transaccion.Commit();
                return idVentaGenerado;
            }
            catch (Exception)
            {
                // Si algo falló, revertimos todo (no quedan ventas huérfanas)
                transaccion.Rollback();
                throw;
            }
        }
    }
}
