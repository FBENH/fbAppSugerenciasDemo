namespace SugerenciasAppLibrary.Logic;

public static class SugerenciaFiltros
{
    public const string TodasLasCategorias = "Todas";
    public const string TodosLosEstados = "Todos";

    public static List<SugerenciaModel> Filtrar(
        IEnumerable<SugerenciaModel> sugerencias,
        string categoria,
        string estado,
        string textoBusqueda,
        bool ordenarPorNuevas)
    {
        IEnumerable<SugerenciaModel> output = sugerencias;

        if (string.IsNullOrEmpty(categoria) == false && categoria != TodasLasCategorias)
        {
            output = output.Where(s => s.Categoria?.CategoriaNombre == categoria);
        }

        if (string.IsNullOrEmpty(estado) == false && estado != TodosLosEstados)
        {
            output = output.Where(s => s.SugerenciaEstado?.EstadoNombre == estado);
        }

        if (string.IsNullOrWhiteSpace(textoBusqueda) == false)
        {
            output = output.Where(s =>
                (s.Sugerencia?.Contains(textoBusqueda, StringComparison.InvariantCultureIgnoreCase) ?? false)
                || (s.Descripcion?.Contains(textoBusqueda, StringComparison.InvariantCultureIgnoreCase) ?? false));
        }

        return Ordenar(output, ordenarPorNuevas);
    }

    public static List<SugerenciaModel> Ordenar(IEnumerable<SugerenciaModel> sugerencias, bool ordenarPorNuevas)
    {
        if (ordenarPorNuevas)
        {
            return sugerencias.OrderByDescending(s => s.FechaCreacion).ToList();
        }

        return sugerencias
            .OrderByDescending(s => s.VotosDeUsuarios?.Count ?? 0)
            .ThenByDescending(s => s.FechaCreacion)
            .ToList();
    }
}
