using PeliculasAPI.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace PeliculasAPI.DTOs
{
    public class GeneroCreacionDTO
    {
        [Required(ErrorMessage = "El campo {0} es requierido")]
        [StringLength(50, ErrorMessage = "El campo {0} no debe tener más de {1} caracteres")]
        [PrimeraLetraMayuscula]
        public required string Nombre { get; set; }
    }
}
