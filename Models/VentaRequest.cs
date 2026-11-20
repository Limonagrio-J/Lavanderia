namespace PrototipoCRUD.Models
{
    public class VentaRequest
    {
        public int IdCliente { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public List<ItemRequest>? Detalles { get; set; }
    }

    public class ItemRequest
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
    }
}
