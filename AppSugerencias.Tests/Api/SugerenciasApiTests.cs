using System.Net;
using System.Net.Http.Json;
using AppSugerencias.Tests.DataAccess;
using AppSugerenciasUI.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppSugerencias.Tests.Api;

[Collection(MongoCollection.Nombre)]
public class SugerenciasApiTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public SugerenciasApiTests(MongoFixture fixture)
    {
        string baseDeDatos = $"api_{Guid.NewGuid():N}";
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
            .UseEnvironment("Testing")
            .UseSetting("ConnectionStrings:MongoDB", fixture.ConnectionString)
            .UseSetting("DatabaseName", baseDeDatos));
    }

    public async ValueTask InitializeAsync()
    {
        _client = _factory.CreateClient();
        // La app carga los datos de ejemplo al arrancar; se espera a que estén.
        for (int i = 0; i < 50 && (await _client.GetFromJsonAsync<List<CategoriaDto>>("/api/categorias")).Count == 0; i++)
        {
            await Task.Delay(100);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ListaSoloLasAprobadas()
    {
        var lista = await _client.GetFromJsonAsync<List<SugerenciaDto>>("/api/sugerencias");

        Assert.Equal(8, lista.Count);
        Assert.DoesNotContain(lista, s => s.Titulo == "Caché distribuida con Redis");
    }

    [Fact]
    public async Task FiltraYOrdenaPorPopularidad()
    {
        var lista = await _client.GetFromJsonAsync<List<SugerenciaDto>>("/api/sugerencias?categoria=Backend&orden=populares");

        Assert.All(lista, s => Assert.Equal("Backend", s.Categoria));
        Assert.Equal(lista.OrderByDescending(s => s.Votos).Select(s => s.Id), lista.Select(s => s.Id));
    }

    [Fact]
    public async Task DetalleYComentarios()
    {
        var lista = await _client.GetFromJsonAsync<List<SugerenciaDto>>("/api/sugerencias?q=jwt");
        var jwt = Assert.Single(lista);

        var detalle = await _client.GetFromJsonAsync<SugerenciaDto>($"/api/sugerencias/{jwt.Id}");
        var comentarios = await _client.GetFromJsonAsync<List<ComentarioDto>>($"/api/sugerencias/{jwt.Id}/comentarios");

        Assert.Equal("Completado", detalle.Estado);
        Assert.StartsWith("https://", detalle.UrlRecurso);
        Assert.Equal(2, comentarios.Count);
    }

    [Fact]
    public async Task NoExponeQuienVoto()
    {
        string json = await _client.GetStringAsync("/api/sugerencias");

        Assert.DoesNotContain("VotosDeUsuarios", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/api/sugerencias/no-es-un-id")]
    [InlineData("/api/sugerencias/000000000000000000000000")]
    [InlineData("/api/sugerencias/000000000000000000000000/comentarios")]
    public async Task DevuelveNotFoundSiNoExiste(string url)
    {
        var respuesta = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task PublicaElDocumentoOpenApi()
    {
        var respuesta = await _client.GetAsync("/openapi/v1.json");

        respuesta.EnsureSuccessStatusCode();
        Assert.Contains("/api/sugerencias/{id}", await respuesta.Content.ReadAsStringAsync());
    }
}
