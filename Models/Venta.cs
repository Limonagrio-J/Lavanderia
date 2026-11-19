namespace PrototipoCRUD.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now; // Default significativo
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = "Consumidor Final";
        public List<DetalleVenta> Detalles { get; set; } = new();

        // Cálculo automático del Total usando LINQ
        public decimal Total => Detalles.Sum(d => d.Subtotal);
    }
}
