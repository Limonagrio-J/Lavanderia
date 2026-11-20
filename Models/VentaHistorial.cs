namespace PrototipoCRUD.Models
{
    public class VentaHistorial
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
        public decimal SubtotalBruto { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal MontoDescuento { get; set; }
        public decimal TotalFinal { get; set; }
        public decimal Total => TotalFinal;
    }
}
