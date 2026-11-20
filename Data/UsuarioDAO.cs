using Npgsql;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Data
{
    public class UsuarioDAO
    {
        private readonly string _cadenaConexion;

        public UsuarioDAO(IConfiguration configuracion)
        {
            _cadenaConexion = configuracion.GetConnectionString("Pepokita")
                ?? throw new InvalidOperationException("Falta la cadena de conexión 'Pepokita'.");
        }

        public Usuario? ObtenerPorNombreUsuario(string nombreUsuario)
        {
            const string sql = @"
                SELECT u.id, u.empleado_id, u.nombre_usuario, u.contrasena_hash, u.activo,
                       u.intentos_fallidos, u.bloqueado, u.ultimo_acceso,
                       COALESCE(e.nombre || ' ' || e.apellido, ''), COALESCE(r.nombre, '')
                  FROM usuarios u
                  JOIN empleados e ON e.id = u.empleado_id
                  JOIN roles r ON r.id = e.rol_id
                 WHERE LOWER(u.nombre_usuario) = LOWER(@nombre)";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("nombre", nombreUsuario);
            using var lector = comando.ExecuteReader();
            if (!lector.Read()) return null;
            return new Usuario
            {
                Id = lector.GetInt32(0),
                EmpleadoId = lector.GetInt32(1),
                NombreUsuario = lector.GetString(2),
                ContrasenaHash = lector.GetString(3),
                Activo = lector.GetBoolean(4),
                IntentosFallidos = lector.GetInt32(5),
                Bloqueado = lector.GetBoolean(6),
                UltimoAcceso = lector.IsDBNull(7) ? null : lector.GetDateTime(7),
                NombreCompleto = lector.GetString(8),
                Rol = lector.GetString(9)
            };
        }

        public void RegistrarIntentoFallido(int usuarioId, int nuevosIntentos, bool bloquear)
        {
            const string sql = @"UPDATE usuarios SET intentos_fallidos = @intentos, bloqueado = @bloqueado WHERE id = @id";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("intentos", nuevosIntentos);
            comando.Parameters.AddWithValue("bloqueado", bloquear);
            comando.Parameters.AddWithValue("id", usuarioId);
            comando.ExecuteNonQuery();
        }

        public void RegistrarAccesoExitoso(int usuarioId)
        {
            const string sql = @"UPDATE usuarios SET intentos_fallidos = 0, bloqueado = FALSE, ultimo_acceso = @ahora WHERE id = @id";
            using var conexion = new NpgsqlConnection(_cadenaConexion);
            conexion.Open();
            using var comando = new NpgsqlCommand(sql, conexion);
            comando.Parameters.AddWithValue("ahora", DateTime.Now);
            comando.Parameters.AddWithValue("id", usuarioId);
            comando.ExecuteNonQuery();
        }
    }
}
