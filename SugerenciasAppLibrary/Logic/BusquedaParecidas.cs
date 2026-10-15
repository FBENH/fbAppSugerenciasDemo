namespace SugerenciasAppLibrary.Logic;

public static class BusquedaParecidas
{
    public const int LargoMinimo = 4;

    // Palabras que aparecen en casi todas las sugerencias y harían que todo parezca parecido.
    private static readonly HashSet<string> PalabrasComunes = new(StringComparer.OrdinalIgnoreCase)
    {
        "curso", "cursos", "clase", "clases", "tutorial", "tutoriales", "sobre", "como", "cómo",
        "para", "con", "una", "uno", "los", "las", "del", "que", "aprender", "introducción", "introduccion"
    };

    /// <summary>
    /// Arma la consulta de texto a partir de lo que escribe el usuario. Devuelve null si no queda nada útil para buscar.
    /// </summary>
    public static string Consulta(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < LargoMinimo)
        {
            return null;
        }

        var terminos = texto
            .Split([' ', ',', '.', ';', ':', '-', '/', '(', ')', '"', '?', '¿', '!', '¡'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 3 && PalabrasComunes.Contains(t) == false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return terminos.Count == 0 ? null : string.Join(' ', terminos);
    }
}
