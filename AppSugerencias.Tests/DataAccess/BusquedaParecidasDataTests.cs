namespace AppSugerencias.Tests.DataAccess;

[Collection(MongoCollection.Nombre)]
public class BusquedaParecidasDataTests(MongoFixture fixture)
{
    private async Task<MongoSugerenciaData> PrepararAsync(params (string Titulo, string Descripcion, bool Aprobada, bool Archivada)[] datos)
    {
        var db = await fixture.NuevaConexionConIndicesAsync();
        await db.SugerenciaColeccion.InsertManyAsync(datos.Select(d => new SugerenciaModel
        {
            Sugerencia = d.Titulo,
            Descripcion = d.Descripcion,
            AprobadoParaMostrar = d.Aprobada,
            Archivado = d.Archivada,
            Autor = new UsuarioBasicoModel { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), DisplayName = "x" },
        }));
        return new MongoSugerenciaData(db, MongoFixture.NuevoCache(), new NotificadorSugerencias());
    }

    [Fact]
    public async Task EncuentraPorPalabrasDelTituloYLaDescripcion()
    {
        var data = await PrepararAsync(
            ("Curso de Docker", "Contenedores para aplicaciones .NET", true, false),
            ("Curso de MongoDB", "Índices y transacciones", true, false),
            ("Curso de Blazor", "Componentes y formularios", true, false));

        var porTitulo = await data.BuscarParecidas("Introducción a docker");
        var porDescripcion = await data.BuscarParecidas("transacciones distribuidas");

        Assert.Equal("Curso de Docker", Assert.Single(porTitulo).Sugerencia);
        Assert.Equal("Curso de MongoDB", Assert.Single(porDescripcion).Sugerencia);
    }

    [Fact]
    public async Task UsaLaRaizDeLasPalabrasEnEspanol()
    {
        var data = await PrepararAsync(("Testear componentes", "Pruebas de interfaz", true, false));

        var resultado = await data.BuscarParecidas("como testeo componente");

        Assert.Single(resultado);
    }

    [Fact]
    public async Task NoMuestraPendientesNiArchivadas()
    {
        var data = await PrepararAsync(
            ("Kubernetes básico", "", false, false),
            ("Kubernetes avanzado", "", true, true));

        Assert.Empty(await data.BuscarParecidas("kubernetes"));
    }

    [Fact]
    public async Task IgnoraPalabrasQueAparecenEnTodasLasSugerencias()
    {
        var data = await PrepararAsync(("Curso de Docker", "", true, false));

        Assert.Empty(await data.BuscarParecidas("curso de react"));
    }
}
