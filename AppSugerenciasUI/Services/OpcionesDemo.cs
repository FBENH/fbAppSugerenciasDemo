namespace AppSugerenciasUI.Services;

public class OpcionesDemo
{
    public const string Seccion = "Demo";

    /// <summary>Carga los datos de ejemplo al arrancar si la base no tiene categorías.</summary>
    public bool SembrarSiVacia { get; set; } = true;

    /// <summary>
    /// Borra y vuelve a cargar los datos de ejemplo cada <see cref="HorasEntreReinicios"/> horas.
    /// Borra todas las sugerencias y comentarios, así que solo tiene sentido en el demo público.
    /// </summary>
    public bool ReinicioAutomatico { get; set; }

    public int HorasEntreReinicios { get; set; } = 24;
}
