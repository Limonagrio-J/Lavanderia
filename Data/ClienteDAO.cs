using Npgsql;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Data
{
    public class ClienteDAO
    {
        private readonly string _cadenaConexion;

        private const string Columnas =
            "id, documento, nombre, apellido, telefono, correo, direccion, activo, tipo_cliente, fecha_registro";

        public ClienteDAO(IConfiguration configuracion)
        {
            _cadenaConexion = configuracion.GetConnectionString("Pepokita")
                ?? throw new InvalidOperationException("Falta la cadena de conexión 'Pepokita'.");
        }

        // Crea la tabla si no existe y agrega las columnas nuevas (tipo y fecha de registro)
        public void AsegurarEstructura()
        {
            const string sql = @"
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
                ALTER TABLE clientes ADD COLUMN IF NOT EXISTS tipo_cliente VARCHAR(20) NOT NULL DEFAULT 'Particular';
                ALTER TABLE clientes ADD COLUMN IF NOT EXISTS fecha_registro DATE NOT NULL DEFAULT CURRENT_DATE;";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            comando.ExecuteNonQuery();
        }

        // CREATE
        public void Insertar(Cliente c)
        {
            const string sql = @"INSERT INTO clientes
                                    (documento, nombre, apellido, telefono, correo, direccion, tipo_cliente, fecha_registro)
                                 VALUES
                                    (@documento, @nombre, @apellido, @telefono, @correo, @direccion, @tipo, @fecha)";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            AgregarParametros(comando, c);
            comando.ExecuteNonQuery();
        }

        // READ (lista de clientes activos)
        public List<Cliente> ObtenerTodos()
        {
            var lista = new List<Cliente>();
            var sql = $"SELECT {Columnas} FROM clientes WHERE activo = TRUE ORDER BY apellido, nombre";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            using var lector = comando.ExecuteReader();
            while (lector.Read())
            {
                lista.Add(Mapear(lector));
            }
            return lista;
        }

        // READ (uno por id)
        public Cliente? ObtenerPorId(int id)
        {
            var sql = $"SELECT {Columnas} FROM clientes WHERE id = @id AND activo = TRUE";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("id", id);
            using var lector = comando.ExecuteReader();
            return lector.Read() ? Mapear(lector) : null;
        }

        // UPDATE
        public void Actualizar(Cliente c)
        {
            const string sql = @"UPDATE clientes
                                 SET documento = @documento, nombre = @nombre, apellido = @apellido,
                                     telefono = @telefono, correo = @correo, direccion = @direccion,
                                     tipo_cliente = @tipo, fecha_registro = @fecha
                                 WHERE id = @id";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            AgregarParametros(comando, c);
            comando.Parameters.AddWithValue("id", c.Id);
            comando.ExecuteNonQuery();
        }

        // DELETE (baja lógica: se conserva el historial del cliente)
        public void Eliminar(int id)
        {
            const string sql = "UPDATE clientes SET activo = FALSE WHERE id = @id";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("id", id);
            comando.ExecuteNonQuery();
        }

        // Validación: el documento es único (incluye clientes dados de baja)
        public bool ExisteDocumento(string documento, int excluirId = 0)
        {
            const string sql = "SELECT COUNT(*) FROM clientes WHERE documento = @documento AND id <> @id";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("documento", documento);
            comando.Parameters.AddWithValue("id", excluirId);
            return Convert.ToInt64(comando.ExecuteScalar()) > 0;
        }

        // Datos para el Panel principal
        public (int Activos, int NuevosMes) Resumen()
        {
            const string sql = @"SELECT
                                    COUNT(*) FILTER (WHERE activo),
                                    COUNT(*) FILTER (WHERE activo
                                        AND date_trunc('month', fecha_registro) = date_trunc('month', CURRENT_DATE))
                                 FROM clientes";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            using var lector = comando.ExecuteReader();
            lector.Read();
            return ((int)lector.GetInt64(0), (int)lector.GetInt64(1));
        }

        private static void AgregarParametros(NpgsqlCommand comando, Cliente c)
        {
            comando.Parameters.AddWithValue("documento", c.Documento);
            comando.Parameters.AddWithValue("nombre", c.Nombre);
            comando.Parameters.AddWithValue("apellido", c.Apellido);
            comando.Parameters.AddWithValue("telefono", c.Telefono);
            comando.Parameters.AddWithValue("correo",
                string.IsNullOrWhiteSpace(c.Correo) ? DBNull.Value : c.Correo);
            comando.Parameters.AddWithValue("direccion",
                string.IsNullOrWhiteSpace(c.Direccion) ? DBNull.Value : c.Direccion);
            comando.Parameters.AddWithValue("tipo", c.TipoCliente);
            comando.Parameters.AddWithValue("fecha", c.FechaRegistro);
        }

        private static Cliente Mapear(NpgsqlDataReader r) => new Cliente
        {
            Id = r.GetInt32(0),
            Documento = r.GetString(1),
            Nombre = r.GetString(2),
            Apellido = r.GetString(3),
            Telefono = r.GetString(4),
            Correo = r.IsDBNull(5) ? null : r.GetString(5),
            Direccion = r.IsDBNull(6) ? null : r.GetString(6),
            Activo = r.GetBoolean(7),
            TipoCliente = r.GetString(8),
            FechaRegistro = r.GetFieldValue<DateOnly>(9)
        };
    }
}
