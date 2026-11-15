namespace PrototipoCRUD.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string TipoCliente { get; set; } = "Particular";
        public DateOnly FechaRegistro { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public bool Activo { get; set; } = true;

        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    }
}
