namespace AppSugerencias.Tests.Logic;

public class SugerenciaFiltrosTests
{
    private static readonly DateTime Base = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly List<SugerenciaModel> Sugerencias =
    [
        Crear("Curso de Blazor", "Componentes y render modes", "Desarrollo", "En revision", dias: 1, votos: 3),
        Crear("Curso de MongoDB", "Transacciones y replica sets", "Bases de datos", "Proximamente", dias: 3, votos: 1),
        Crear("Curso de Docker", "Contenedores para .NET", "Desarrollo", null, dias: 2, votos: 5),
    ];

    private static SugerenciaModel Crear(string titulo, string descripcion, string categoria, string estado, int dias, int votos)
    {
        return new SugerenciaModel
        {
            Sugerencia = titulo,
            Descripcion = descripcion,
            Categoria = new CategoriaModel { CategoriaNombre = categoria },
            SugerenciaEstado = estado is null ? null : new EstadoModel { EstadoNombre = estado },
            FechaCreacion = Base.AddDays(dias),
            VotosDeUsuarios = Enumerable.Range(0, votos).Select(i => $"u{i}").ToHashSet()
        };
    }

    private static List<string> Titulos(List<SugerenciaModel> lista) => lista.Select(s => s.Sugerencia).ToList();

    [Fact]
    public void SinFiltros_OrdenaPorMasNuevas()
    {
        var resultado = SugerenciaFiltros.Filtrar(Sugerencias, "Todas", "Todos", "", ordenarPorNuevas: true);

        Assert.Equal(["Curso de MongoDB", "Curso de Docker", "Curso de Blazor"], Titulos(resultado));
    }

    [Fact]
    public void OrdenarPorVotos_DesempataPorFecha()
    {
        var empate = Crear("Curso de Azure", "", "Cloud", null, dias: 4, votos: 5);

        var resultado = SugerenciaFiltros.Filtrar([.. Sugerencias, empate], "Todas", "Todos", null, ordenarPorNuevas: false);

        Assert.Equal(["Curso de Azure", "Curso de Docker", "Curso de Blazor", "Curso de MongoDB"], Titulos(resultado));
    }

    [Fact]
    public void FiltraPorCategoria()
    {
        var resultado = SugerenciaFiltros.Filtrar(Sugerencias, "Desarrollo", "Todos", "", ordenarPorNuevas: true);

        Assert.Equal(["Curso de Docker", "Curso de Blazor"], Titulos(resultado));
    }

    [Fact]
    public void FiltraPorEstado()
    {
        var resultado = SugerenciaFiltros.Filtrar(Sugerencias, "Todas", "Proximamente", "", ordenarPorNuevas: true);

        Assert.Equal(["Curso de MongoDB"], Titulos(resultado));
    }

    [Theory]
    [InlineData("docker")]
    [InlineData("CONTENEDORES")]
    public void BuscaEnTituloYDescripcionSinDistinguirMayusculas(string texto)
    {
        var resultado = SugerenciaFiltros.Filtrar(Sugerencias, "Todas", "Todos", texto, ordenarPorNuevas: true);

        Assert.Equal(["Curso de Docker"], Titulos(resultado));
    }

    [Fact]
    public void BusquedaToleraDescripcionVacia()
    {
        var sinDescripcion = Crear("Curso de Git", null, "Desarrollo", null, dias: 5, votos: 0);

        var resultado = SugerenciaFiltros.Filtrar([sinDescripcion], "Todas", "Todos", "git", ordenarPorNuevas: true);

        Assert.Single(resultado);
    }
}
