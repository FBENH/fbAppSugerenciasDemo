namespace AppSugerencias.Tests.DataAccess;

[Collection(MongoCollection.Nombre)]
public class MongoSugerenciaDataTests
{
    private readonly IDBConnection _db;
    private readonly MongoUserData _usuarios;
    private readonly MongoSugerenciaData _sugerencias;
    private readonly NotificadorSugerencias _notificador = new();
    private readonly List<CambioSugerencia> _cambios = [];

    public MongoSugerenciaDataTests(MongoFixture fixture)
    {
        _db = fixture.NuevaConexion();
        _usuarios = new MongoUserData(_db);
        _sugerencias = new MongoSugerenciaData(_db, MongoFixture.NuevoCache(), _notificador);
        _notificador.Cambio += c =>
        {
            lock (_cambios) { _cambios.Add(c); }
            return Task.CompletedTask;
        };
    }

    private async Task<UsuarioModel> CrearUsuario(string nombre)
    {
        var usuario = new UsuarioModel { ObjectIdentifier = Guid.NewGuid().ToString(), DisplayName = nombre };
        await _usuarios.CreateUsuario(usuario);
        return usuario;
    }

    private async Task<SugerenciaModel> CrearSugerencia(UsuarioModel autor, string titulo = "Curso de Blazor", string descripcion = "Descripción")
    {
        var sugerencia = new SugerenciaModel
        {
            Sugerencia = titulo,
            Descripcion = descripcion,
            Autor = new UsuarioBasicoModel(autor),
            Categoria = new CategoriaModel { CategoriaNombre = "Desarrollo" }
        };
        await _sugerencias.CreateSugerencia(sugerencia);
        return sugerencia;
    }

    [Fact]
    public async Task CreateSugerencia_GuardaLaSugerenciaYLaAgregaAlAutor()
    {
        var autor = await CrearUsuario("Autor");

        var sugerencia = await CrearSugerencia(autor);

        var guardada = await _sugerencias.GetSugerencia(sugerencia.Id);
        Assert.Equal("Curso de Blazor", guardada.Sugerencia);
        var autorGuardado = await _usuarios.GetUsuario(autor.Id);
        Assert.Equal(sugerencia.Id, Assert.Single(autorGuardado.SugerenciasCreadas).Id);
    }

    [Fact]
    public async Task VotarSugerencia_AlternaElVotoEnLaSugerenciaYEnElUsuario()
    {
        var autor = await CrearUsuario("Autor");
        var votante = await CrearUsuario("Votante");
        var sugerencia = await CrearSugerencia(autor);

        await _sugerencias.VotarSugerencia(sugerencia.Id, votante.Id);

        Assert.Contains(votante.Id, (await _sugerencias.GetSugerencia(sugerencia.Id)).VotosDeUsuarios);
        Assert.Equal(sugerencia.Id, Assert.Single((await _usuarios.GetUsuario(votante.Id)).SugerenciasVotadas).Id);

        await _sugerencias.VotarSugerencia(sugerencia.Id, votante.Id);

        Assert.Empty((await _sugerencias.GetSugerencia(sugerencia.Id)).VotosDeUsuarios);
        Assert.Empty((await _usuarios.GetUsuario(votante.Id)).SugerenciasVotadas);
    }

    [Fact]
    public async Task VotarSugerencia_IgnoraElVotoDelAutor()
    {
        var autor = await CrearUsuario("Autor");
        var sugerencia = await CrearSugerencia(autor);

        await _sugerencias.VotarSugerencia(sugerencia.Id, autor.Id);

        Assert.Empty((await _sugerencias.GetSugerencia(sugerencia.Id)).VotosDeUsuarios);
        Assert.Empty((await _usuarios.GetUsuario(autor.Id)).SugerenciasVotadas);
    }

    [Fact]
    public async Task VotarSugerencia_SoportaVotosSimultaneosALaMismaSugerencia()
    {
        var autor = await CrearUsuario("Autor");
        var sugerencia = await CrearSugerencia(autor);
        var votantes = new List<UsuarioModel>();
        for (int i = 0; i < 8; i++)
        {
            votantes.Add(await CrearUsuario($"Votante {i}"));
        }

        // Cada votante hace clic tres veces seguidas: termina con el voto puesto.
        await Task.WhenAll(votantes.SelectMany(v => Enumerable.Range(0, 3).Select(_ => _sugerencias.VotarSugerencia(sugerencia.Id, v.Id))));

        var guardada = await _sugerencias.GetSugerencia(sugerencia.Id);
        Assert.Equal(votantes.Select(v => v.Id).ToHashSet(), guardada.VotosDeUsuarios);
        foreach (var votante in votantes)
        {
            Assert.Single((await _usuarios.GetUsuario(votante.Id)).SugerenciasVotadas);
        }
    }

    [Fact]
    public async Task Aprobacion_SoloMuestraLasAprobadasYLasPendientesVanAlAdmin()
    {
        var autor = await CrearUsuario("Autor");
        var aprobada = await CrearSugerencia(autor, "Aprobada");
        var pendiente = await CrearSugerencia(autor, "Pendiente");
        var rechazada = await CrearSugerencia(autor, "Rechazada");

        aprobada.AprobadoParaMostrar = true;
        await _sugerencias.UpdateSugerencia(aprobada);
        rechazada.Rechazado = true;
        await _sugerencias.UpdateSugerencia(rechazada);

        Assert.Equal(aprobada.Id, Assert.Single(await _sugerencias.GetAllSugerenciasAprobadas()).Id);
        Assert.Equal(pendiente.Id, Assert.Single(await _sugerencias.GetAllSugerenciasEsperandoAprobacion()).Id);
    }

    [Fact]
    public async Task UpdateSugerencia_ArchivarLaSacaDelListado()
    {
        var autor = await CrearUsuario("Autor");
        var sugerencia = await CrearSugerencia(autor);
        sugerencia.AprobadoParaMostrar = true;
        await _sugerencias.UpdateSugerencia(sugerencia);
        Assert.Single(await _sugerencias.GetAllSugerenciasAprobadas());

        sugerencia.Archivado = true;
        await _sugerencias.UpdateSugerencia(sugerencia);

        Assert.Empty(await _sugerencias.GetAllSugerenciasAprobadas());
    }

    [Fact]
    public async Task CadaCambioAvisaALasPantallasAbiertas()
    {
        var autor = await CrearUsuario("Autor");
        var votante = await CrearUsuario("Votante");

        var sugerencia = await CrearSugerencia(autor);
        await _sugerencias.VotarSugerencia(sugerencia.Id, votante.Id);
        sugerencia.AprobadoParaMostrar = true;
        await _sugerencias.UpdateSugerencia(sugerencia);

        Assert.Equal(
            [TipoCambio.Creada, TipoCambio.Voto, TipoCambio.Actualizada],
            _cambios.Select(c => c.Tipo));
        Assert.All(_cambios, c => Assert.Equal(sugerencia.Id, c.SugerenciaId));
    }

    [Fact]
    public async Task UpdateSugerencia_ElPerfilDelAutorVeLosCambiosSinEsperarElCache()
    {
        var autor = await CrearUsuario("Autor");
        var sugerencia = await CrearSugerencia(autor);
        Assert.False(Assert.Single(await _sugerencias.GetSugerenciasUsuario(autor.Id)).AprobadoParaMostrar);

        sugerencia.AprobadoParaMostrar = true;
        await _sugerencias.UpdateSugerencia(sugerencia);

        Assert.True(Assert.Single(await _sugerencias.GetSugerenciasUsuario(autor.Id)).AprobadoParaMostrar);
    }
}
