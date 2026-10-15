namespace AppSugerencias.Tests.DataAccess;

[Collection(MongoCollection.Nombre)]
public class MongoComentarioDataTests(MongoFixture fixture)
{
    private readonly string _sugerenciaId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

    private static ComentarioModel Comentario(string sugerenciaId, string texto, DateTime fecha) => new()
    {
        SugerenciaId = sugerenciaId,
        Texto = texto,
        Fecha = fecha,
        Autor = new UsuarioBasicoModel { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), DisplayName = "Ana" },
    };

    [Fact]
    public async Task ListaLosComentariosDeLaSugerenciaEnOrdenDeFecha()
    {
        var data = new MongoComentarioData(fixture.NuevaConexion(), new NotificadorSugerencias());
        var hoy = DateTime.UtcNow;

        await data.CrearComentario(Comentario(_sugerenciaId, "segundo", hoy));
        await data.CrearComentario(Comentario(_sugerenciaId, "primero", hoy.AddMinutes(-5)));
        await data.CrearComentario(Comentario(MongoDB.Bson.ObjectId.GenerateNewId().ToString(), "de otra", hoy));

        var lista = await data.GetComentarios(_sugerenciaId);

        Assert.Equal(["primero", "segundo"], lista.Select(c => c.Texto));
    }

    [Fact]
    public async Task RecortaEspaciosYElLargoMaximo()
    {
        var data = new MongoComentarioData(fixture.NuevaConexion(), new NotificadorSugerencias());

        await data.CrearComentario(Comentario(_sugerenciaId, "  hola  ", DateTime.UtcNow));
        await data.CrearComentario(Comentario(_sugerenciaId, new string('a', 1500), DateTime.UtcNow.AddSeconds(1)));

        var lista = await data.GetComentarios(_sugerenciaId);
        Assert.Equal("hola", lista[0].Texto);
        Assert.Equal(ComentarioModel.LargoMaximo, lista[1].Texto.Length);
    }

    [Fact]
    public async Task RechazaComentariosVacios()
    {
        var data = new MongoComentarioData(fixture.NuevaConexion(), new NotificadorSugerencias());

        await Assert.ThrowsAsync<ArgumentException>(() => data.CrearComentario(Comentario(_sugerenciaId, "   ", DateTime.UtcNow)));
    }

    [Fact]
    public async Task EliminarAvisaALaPantallaDeLaSugerencia()
    {
        var notificador = new NotificadorSugerencias();
        var cambios = new List<CambioSugerencia>();
        notificador.Cambio += c => { cambios.Add(c); return Task.CompletedTask; };
        var data = new MongoComentarioData(fixture.NuevaConexion(), notificador);
        var comentario = Comentario(_sugerenciaId, "spam", DateTime.UtcNow);
        await data.CrearComentario(comentario);

        await data.EliminarComentario(comentario.Id);

        Assert.Empty(await data.GetComentarios(_sugerenciaId));
        Assert.Equal(2, cambios.Count);
        Assert.All(cambios, c => Assert.Equal(new CambioSugerencia(_sugerenciaId, TipoCambio.Comentario), c));
    }
}
