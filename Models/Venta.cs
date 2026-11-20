namespace PrototipoCRUD.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int IdCliente { get; set; }
        public int? IdVendedor { get; set; }
        public string Vendedor { get; set; } = string.Empty;
        public List<DetalleVenta> Detalles { get; set; } = new();

        public decimal PorcentajeDescuento { get; private set; }

        public decimal SubtotalBruto => Detalles.Sum(d => d.Subtotal);

        public decimal MontoDescuento => Math.Round(
            SubtotalBruto * (PorcentajeDescuento / 100m), 2, MidpointRounding.AwayFromZero);

        public decimal TotalFinal => SubtotalBruto - MontoDescuento;

        public decimal Total => TotalFinal;

        public void AplicarDescuento(decimal porcentaje)
        {
            if (porcentaje < 0 || porcentaje > 20)
            {
                throw new ArgumentOutOfRangeException(nameof(porcentaje),
                    "El descuento máximo permitido es 20%. Ingresa un valor entre 0% y 20% para continuar.");
            }
            PorcentajeDescuento = porcentaje;
        }
    }
}
