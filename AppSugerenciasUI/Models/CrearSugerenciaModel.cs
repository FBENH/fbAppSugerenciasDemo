using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AppSugerenciasUI.Models;

public class CrearSugerenciaModel
{
    [Required(ErrorMessage = "El campo sugerencia es obligatorio. Por favor ingrese una sugerencia.")]
    [MaxLength(75)]
    public string Sugerencia { get; set; }
    [Required(ErrorMessage = "Debe seleccionar una categoría.")]
    [MinLength(1)]
    [Display(Name="Categoría")]
    public string CategoriaId { get; set; }
    [MaxLength(500)]
    public string Descripcion { get; set; }
}
