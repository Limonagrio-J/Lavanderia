namespace PrototipoCRUD.Models
{
    // Una línea (producto) de una venta ya registrada
    public class LineaHistorial
    {
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    // Una venta ya registrada, con su cliente y sus líneas
    public class VentaHistorial
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public List<LineaHistorial> Lineas { get; set; } = new();
    }

    public class ResumenCliente
    {
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public int Compras { get; set; }
        public decimal Total { get; set; }
        public DateTime UltimaCompra { get; set; }
    }

    public class ResumenProducto
    {
        public string Producto { get; set; } = string.Empty;
        public int Unidades { get; set; }
        public int Ventas { get; set; }
        public decimal Ingresos { get; set; }
    }

    public class HistorialViewModel
    {
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
        public int IdCliente { get; set; }

        public List<VentaHistorial> Ventas { get; set; } = new();
        public List<Cliente> ClientesFiltro { get; set; } = new();

        public decimal Ingresos => Ventas.Sum(v => v.Total);
        public int Unidades => Ventas.Sum(v => v.Lineas.Sum(l => l.Cantidad));
        public decimal TicketPromedio => Ventas.Count == 0 ? 0m : Ingresos / Ventas.Count;

        public List<ResumenCliente> PorCliente => Ventas
            .GroupBy(v => v.IdCliente)
            .Select(g => new ResumenCliente
            {
                IdCliente = g.Key,
                Cliente = g.First().Cliente,
                Documento = g.First().Documento,
                Compras = g.Count(),
                Total = g.Sum(v => v.Total),
                UltimaCompra = g.Max(v => v.Fecha)
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        public List<ResumenProducto> PorProducto => Ventas
            .SelectMany(v => v.Lineas.Select(l => new { VentaId = v.Id, Linea = l }))
            .GroupBy(x => x.Linea.Producto)
            .Select(g => new ResumenProducto
            {
                Producto = g.Key,
                Unidades = g.Sum(x => x.Linea.Cantidad),
                Ventas = g.Select(x => x.VentaId).Distinct().Count(),
                Ingresos = g.Sum(x => x.Linea.Subtotal)
            })
            .OrderByDescending(p => p.Unidades)
            .ThenBy(p => p.Producto)
            .ToList();
    }
}
