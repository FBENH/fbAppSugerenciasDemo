using System.Text.RegularExpressions;

namespace SugerenciasAppLibrary.Logic;

public static partial class NotaAdmin
{
    public const string TextoCompletado = "Creamos un recurso sobre ese tema aquí:";

    /// <summary>
    /// Acepta solo URLs absolutas http/https, para que el link de "Completado" no pueda
    /// llevar javascript: u otros esquemas.
    /// </summary>
    public static bool TryCrearUrlRecurso(string input, out string url)
    {
        url = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (Uri.TryCreate(input.Trim(), UriKind.Absolute, out Uri uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            url = uri.AbsoluteUri;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Devuelve el texto y el link a mostrar. Las notas guardadas antes de agregar
    /// <see cref="SugerenciaModel.UrlRecurso"/> tienen el link como HTML dentro del texto;
    /// en ese caso se separa el texto del link en lugar de renderizar el HTML.
    /// </summary>
    public static (string Texto, string Url) Leer(SugerenciaModel sugerencia)
    {
        string notas = sugerencia.NotasAdmins ?? "";
        string url = sugerencia.UrlRecurso;

        int inicioHtml = notas.IndexOf("<a ", StringComparison.OrdinalIgnoreCase);
        if (inicioHtml >= 0)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Match match = HrefLegado().Match(notas);
                url = match.Success && TryCrearUrlRecurso(match.Groups[1].Value, out string valida) ? valida : null;
            }

            notas = notas[..inicioHtml].Trim();
        }

        return (notas, string.IsNullOrWhiteSpace(url) ? null : url);
    }

    [GeneratedRegex("""href=["']?([^"'\s>]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex HrefLegado();
}
