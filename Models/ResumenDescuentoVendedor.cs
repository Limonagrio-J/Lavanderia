namespace PrototipoCRUD.Models
{
    public class ResumenDescuentoVendedor
    {
        public string Vendedor { get; set; } = string.Empty;
        public int VentasConDescuento { get; set; }
        public decimal TotalDescontado { get; set; }
    }
}
