namespace PrototipoCRUD.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string ContrasenaHash { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int IntentosFallidos { get; set; }
        public bool Bloqueado { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;

        public bool PuedeAplicarDescuento() => Rol == "Vendedor" || Rol == "Administrador";
        public bool PuedeVerAuditoria() => Rol == "Administrador" || Rol == "Gerente";
    }
}
