using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AppSugerenciasUI.Models;

public class CrearSugerenciaModel
{
    [Required(ErrorMessage = "Escribí el tema de la sugerencia.")]
    [MaxLength(75, ErrorMessage = "El tema puede tener hasta 75 caracteres.")]
    public string Sugerencia { get; set; }
    [Required(ErrorMessage = "Elegí una categoría.")]
    [MinLength(1)]
    [Display(Name="Categoría")]
    public string CategoriaId { get; set; }
    [MaxLength(500, ErrorMessage = "La descripción puede tener hasta 500 caracteres.")]
    public string Descripcion { get; set; }
}
