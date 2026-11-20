using PrototipoCRUD.Services;
using Xunit;

namespace PrototipoCRUD.Tests
{
    public class AutenticacionTests
    {
        private const string HashCperez = "v1$JU6Ff5stfcumJuRafzaB2Q==$pOnR6xCWUYsaLgtoDEONPxrl1xxRZVcTKB6Aa6V87Lg=";

        [Fact]
        public void VerificarContrasena_Correcta_DevuelveTrue()
        {
            Assert.True(ServicioAutenticacion.VerificarContrasena("Vendedor123!", HashCperez));
        }

        [Fact]
        public void VerificarContrasena_Incorrecta_DevuelveFalse()
        {
            Assert.False(ServicioAutenticacion.VerificarContrasena("incorrecta", HashCperez));
        }

        [Fact]
        public void VerificarContrasena_HashMalformado_DevuelveFalse()
        {
            Assert.False(ServicioAutenticacion.VerificarContrasena("Vendedor123!", "sin-formato"));
            Assert.False(ServicioAutenticacion.VerificarContrasena("Vendedor123!", ""));
            Assert.False(ServicioAutenticacion.VerificarContrasena("Vendedor123!", "v1$$"));
        }
    }
}
