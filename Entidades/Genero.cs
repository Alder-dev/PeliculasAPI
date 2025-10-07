using PeliculasAPI.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace PeliculasAPI.Entidades
{
    public class Genero: IId
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo {0} es requierido")]
        [StringLength(50, ErrorMessage = "El campo {0} no debe tener más de {1} caracteres")]
        [PrimeraLetraMayuscula]
        public required string Nombre { get; set; }
    }
}
