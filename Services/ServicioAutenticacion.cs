using System.Security.Cryptography;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Services
{
    public enum ResultadoLogin
    {
        Exito,
        CredencialesInvalidas,
        Bloqueado,
        Inactivo
    }

    public class ServicioAutenticacion
    {
        private readonly UsuarioDAO _usuarioDao;

        public ServicioAutenticacion(UsuarioDAO usuarioDao)
        {
            _usuarioDao = usuarioDao;
        }

        public (ResultadoLogin resultado, Usuario? usuario) Autenticar(string nombreUsuario, string contrasena)
        {
            var usuario = _usuarioDao.ObtenerPorNombreUsuario(nombreUsuario ?? string.Empty);
            if (usuario == null)
                return (ResultadoLogin.CredencialesInvalidas, null);
            if (!usuario.Activo)
                return (ResultadoLogin.Inactivo, null);
            if (usuario.Bloqueado)
                return (ResultadoLogin.Bloqueado, null);

            if (!VerificarContrasena(contrasena ?? string.Empty, usuario.ContrasenaHash))
            {
                int intentos = usuario.IntentosFallidos + 1;
                bool bloquear = intentos >= 5;
                _usuarioDao.RegistrarIntentoFallido(usuario.Id, intentos, bloquear);
                return (bloquear ? ResultadoLogin.Bloqueado : ResultadoLogin.CredencialesInvalidas, null);
            }

            _usuarioDao.RegistrarAccesoExitoso(usuario.Id);
            return (ResultadoLogin.Exito, usuario);
        }

        public static bool VerificarContrasena(string contrasena, string hashAlmacenado)
        {
            if (string.IsNullOrWhiteSpace(hashAlmacenado)) return false;
            try
            {
                // Formato: v1$base64(salt)$base64(hash)
                var partes = hashAlmacenado.Split('$');
                if (partes.Length == 3 && partes[0] == "v1")
                {
                    if (string.IsNullOrEmpty(partes[1]) || string.IsNullOrEmpty(partes[2])) return false;
                    var salt = Convert.FromBase64String(partes[1]);
                    var hash = Convert.FromBase64String(partes[2]);
                    if (salt.Length == 0 || hash.Length == 0) return false;
                    var calculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, salt, 100000, HashAlgorithmName.SHA256, hash.Length);
                    return CryptographicOperations.FixedTimeEquals(calculado, hash);
                }
            }
            catch
            {
                return false;
            }
            return false;
        }
    }
}
