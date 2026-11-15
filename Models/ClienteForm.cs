namespace PrototipoCRUD.Models
{
    /// <summary>
    /// Datos tal como llegan desde el diálogo (todo texto). El controlador los valida
    /// con mensajes amables y luego los convierte a un Cliente.
    /// </summary>
    public class ClienteForm
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Documento { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? TipoCliente { get; set; }
        public string? FechaRegistro { get; set; }   // formato yyyy-MM-dd
    }
}
