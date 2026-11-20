using PrototipoCRUD.Models;
using Xunit;

namespace PrototipoCRUD.Tests
{
    public class VentaDescuentoTests
    {
        private static Venta CrearVenta(decimal precio, int cantidad = 1)
        {
            var venta = new Venta();
            venta.Detalles.Add(new DetalleVenta
            {
                IdProducto = 1,
                NombreProducto = "Lavado Exterior",
                Cantidad = cantidad,
                PrecioUnitario = precio
            });
            return venta;
        }

        [Fact]
        public void AplicarDescuento_15Porciento_Valido_CalculaMontosCorrectos()
        {
            var venta = CrearVenta(200.00m);
            venta.AplicarDescuento(15m);

            Assert.Equal(200.00m, venta.SubtotalBruto);
            Assert.Equal(15m, venta.PorcentajeDescuento);
            Assert.Equal(30.00m, venta.MontoDescuento);
            Assert.Equal(170.00m, venta.TotalFinal);
            Assert.Equal(venta.TotalFinal, venta.Total);
        }

        [Fact]
        public void AplicarDescuento_20Porciento_Borde_Aceptado()
        {
            var venta = CrearVenta(100.00m);
            venta.AplicarDescuento(20m);

            Assert.Equal(20.00m, venta.MontoDescuento);
            Assert.Equal(80.00m, venta.TotalFinal);
        }

        [Fact]
        public void AplicarDescuento_MayorA20_LanzaArgumentOutOfRangeException()
        {
            var venta = CrearVenta(200.00m);
            Assert.Throws<ArgumentOutOfRangeException>(() => venta.AplicarDescuento(25m));
        }

        [Fact]
        public void AplicarDescuento_Negativo_LanzaArgumentOutOfRangeException()
        {
            var venta = CrearVenta(200.00m);
            Assert.Throws<ArgumentOutOfRangeException>(() => venta.AplicarDescuento(-5m));
        }

        [Fact]
        public void AplicarDescuento_Cero_SinDescuento()
        {
            var venta = CrearVenta(150.00m);
            venta.AplicarDescuento(0m);

            Assert.Equal(150.00m, venta.SubtotalBruto);
            Assert.Equal(0.00m, venta.MontoDescuento);
            Assert.Equal(150.00m, venta.TotalFinal);
        }

        [Fact]
        public void AplicarDescuento_Redondeo_AwayFromZero()
        {
            var venta = CrearVenta(33.33m);
            venta.AplicarDescuento(10m);

            Assert.Equal(33.33m, venta.SubtotalBruto);
            Assert.Equal(3.33m, venta.MontoDescuento);
            Assert.Equal(30.00m, venta.TotalFinal);
        }

        [Fact]
        public void SubtotalBruto_SumaMultiplesDetalles()
        {
            var venta = new Venta();
            venta.Detalles.Add(new DetalleVenta { IdProducto = 1, NombreProducto = "A", Cantidad = 2, PrecioUnitario = 30.00m });
            venta.Detalles.Add(new DetalleVenta { IdProducto = 2, NombreProducto = "B", Cantidad = 1, PrecioUnitario = 60.00m });
            venta.AplicarDescuento(10m);

            Assert.Equal(120.00m, venta.SubtotalBruto);
            Assert.Equal(12.00m, venta.MontoDescuento);
            Assert.Equal(108.00m, venta.TotalFinal);
        }

        [Fact]
        public void Venta_PorDefecto_VendedorVacioYDescuentoCero()
        {
            var venta = new Venta();
            Assert.Equal(string.Empty, venta.Vendedor);
            Assert.Equal(0m, venta.PorcentajeDescuento);
            Assert.Empty(venta.Detalles);
        }
    }
}
