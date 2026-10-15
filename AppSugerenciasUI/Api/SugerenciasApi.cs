namespace AppSugerenciasUI.Api;

public record SugerenciaDto(
    string Id, string Titulo, string Descripcion, string Categoria, string Estado,
    string NotaAdmin, string UrlRecurso, string Autor, DateTime FechaCreacion, int Votos);

public record ComentarioDto(string Id, string Autor, bool EsAdmin, string Texto, DateTime Fecha);

public record CategoriaDto(string Id, string Nombre, string Descripcion);

public record EstadoDto(string Id, string Nombre, string Descripcion);

/// <summary>
/// API de solo lectura sobre las sugerencias aprobadas. No expone quién votó cada sugerencia, solo la cantidad.
/// </summary>
public static class SugerenciasApi
{
    public static IEndpointRouteBuilder MapSugerenciasApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").WithTags("Sugerencias").AllowAnonymous();

        api.MapGet("/sugerencias", ListarSugerencias)
            .WithSummary("Lista las sugerencias aprobadas")
            .WithDescription("Filtra por categoría, estado y texto. `orden` puede ser `nuevas` (por defecto) o `populares`.");

        api.MapGet("/sugerencias/{id}", ObtenerSugerencia)
            .WithSummary("Obtiene una sugerencia aprobada");

        api.MapGet("/sugerencias/{id}/comentarios", ListarComentarios)
            .WithSummary("Lista los comentarios de una sugerencia aprobada");

        api.MapGet("/categorias", async (ICategoryData data) =>
                (await data.GetAllCategorias()).Select(c => new CategoriaDto(c.Id, c.CategoriaNombre, c.CategoriaDescripcion)))
            .WithSummary("Lista las categorías");

        api.MapGet("/estados", async (IStatusData data) =>
                (await data.GetAllEstados()).Select(e => new EstadoDto(e.Id, e.EstadoNombre, e.EstadoDescripcion)))
            .WithSummary("Lista los estados posibles");

        return app;
    }

    public static async Task<IEnumerable<SugerenciaDto>> ListarSugerencias(
        IMongoSugerenciaData data, string categoria = null, string estado = null, string q = null, string orden = null)
    {
        var aprobadas = await data.GetAllSugerenciasAprobadas();
        bool porNuevas = string.Equals(orden, "populares", StringComparison.OrdinalIgnoreCase) == false;
        return SugerenciaFiltros.Filtrar(aprobadas, categoria, estado, q, porNuevas).Select(ADto);
    }

    public static async Task<IResult> ObtenerSugerencia(string id, IMongoSugerenciaData data)
    {
        var sugerencia = await BuscarVisible(id, data);
        return sugerencia is null ? TypedResults.NotFound() : TypedResults.Ok(ADto(sugerencia));
    }

    public static async Task<IResult> ListarComentarios(string id, IMongoSugerenciaData data, IComentarioData comentarios)
    {
        var sugerencia = await BuscarVisible(id, data);
        if (sugerencia is null)
        {
            return TypedResults.NotFound();
        }

        var lista = await comentarios.GetComentarios(id);
        return TypedResults.Ok(lista.Select(c => new ComentarioDto(c.Id, c.Autor?.DisplayName, c.EsAdmin, c.Texto, c.Fecha)));
    }

    private static async Task<SugerenciaModel> BuscarVisible(string id, IMongoSugerenciaData data)
    {
        if (MongoDB.Bson.ObjectId.TryParse(id, out _) == false)
        {
            return null;
        }

        var sugerencia = await data.GetSugerencia(id);
        return sugerencia is { AprobadoParaMostrar: true, Archivado: false } ? sugerencia : null;
    }

    private static SugerenciaDto ADto(SugerenciaModel s)
    {
        var (nota, url) = NotaAdmin.Leer(s);
        return new SugerenciaDto(
            s.Id, s.Sugerencia, s.Descripcion, s.Categoria?.CategoriaNombre, s.SugerenciaEstado?.EstadoNombre,
            string.IsNullOrEmpty(nota) ? null : nota, url, s.Autor?.DisplayName, s.FechaCreacion, s.VotosDeUsuarios?.Count ?? 0);
    }
}
