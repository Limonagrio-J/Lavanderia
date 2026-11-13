using System.ComponentModel.DataAnnotations;

namespace PrototipoCRUD.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El documento es obligatorio.")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        [Display(Name = "Documento (CI)")]
        public string Documento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
        public string? Correo { get; set; }

        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        public bool Activo { get; set; } = true;
    }
}
