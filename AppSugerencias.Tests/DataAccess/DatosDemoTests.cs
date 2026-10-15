using AppSugerenciasUI.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace AppSugerencias.Tests.DataAccess;

[Collection(MongoCollection.Nombre)]
public class DatosDemoTests(MongoFixture fixture)
{
    [Fact]
    public async Task SembrarSiVacia_CargaDatosCoherentesSoloUnaVez()
    {
        var db = fixture.NuevaConexion();

        Assert.True(await DatosDemo.SembrarSiVaciaAsync(db));
        Assert.False(await DatosDemo.SembrarSiVaciaAsync(db));

        var sugerencias = await db.SugerenciaColeccion.Find(_ => true).ToListAsync();
        var usuarios = await db.UsuarioColeccion.Find(_ => true).ToListAsync();
        Assert.NotEmpty(sugerencias);
        Assert.Contains(sugerencias, s => s.AprobadoParaMostrar == false);

        // Cada voto de una sugerencia tiene que figurar en las votadas del usuario, y nadie vota la suya.
        foreach (var s in sugerencias)
        {
            Assert.DoesNotContain(s.Autor.Id, s.VotosDeUsuarios);
            foreach (string votante in s.VotosDeUsuarios)
            {
                Assert.Contains(usuarios.Single(u => u.Id == votante).SugerenciasVotadas, v => v.Id == s.Id);
            }
        }
    }

    [Fact]
    public async Task Reiniciar_BorraLoQueAgreganLosVisitantesYConservaLosUsuariosReales()
    {
        var db = fixture.NuevaConexion();
        await DatosDemo.SembrarSiVaciaAsync(db);
        int cantidadOriginal = (int)await db.SugerenciaColeccion.CountDocumentsAsync(_ => true);

        var real = new UsuarioModel { ObjectIdentifier = "b2c-real", DisplayName = "Visitante" };
        await db.UsuarioColeccion.InsertOneAsync(real);
        var nueva = new SugerenciaModel { Sugerencia = "Agregada por un visitante", Autor = new UsuarioBasicoModel(real) };
        await db.SugerenciaColeccion.InsertOneAsync(nueva);
        real.SugerenciasCreadas.Add(new SugerenciaBasicaModel(nueva));
        await db.UsuarioColeccion.ReplaceOneAsync(u => u.Id == real.Id, real);
        await db.SugerenciaColeccion.UpdateManyAsync(_ => true, Builders<SugerenciaModel>.Update.Set(s => s.Archivado, true));

        await DatosDemo.ReiniciarAsync(db, DateTime.UtcNow);

        var sugerencias = await db.SugerenciaColeccion.Find(_ => true).ToListAsync();
        Assert.Equal(cantidadOriginal, sugerencias.Count);
        Assert.DoesNotContain(sugerencias, s => s.Archivado || s.Sugerencia == nueva.Sugerencia);
        var realDespues = await db.UsuarioColeccion.Find(u => u.Id == real.Id).SingleAsync();
        Assert.Empty(realDespues.SugerenciasCreadas);
        Assert.Equal(6, await db.UsuarioColeccion.CountDocumentsAsync(u => u.ObjectIdentifier.StartsWith(DatosDemo.PrefijoUsuarioDemo)));
    }

    [Fact]
    public async Task Mantenimiento_ReiniciaSoloCuandoPasoElIntervalo()
    {
        var db = fixture.NuevaConexion();
        await DatosDemo.SembrarSiVaciaAsync(db);
        var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 3, 0, 0, TimeSpan.Zero));
        var servicio = new MantenimientoDemoService(
            db, new MemoryCache(new MemoryCacheOptions()), new NotificadorSugerencias(),
            Options.Create(new OpcionesDemo { ReinicioAutomatico = true, HorasEntreReinicios = 24 }),
            reloj, NullLogger<MantenimientoDemoService>.Instance);
        await db.SugerenciaColeccion.InsertOneAsync(new SugerenciaModel { Sugerencia = "Del visitante" });

        Assert.False(await servicio.ReiniciarSiCorrespondeAsync()); // primera vez: solo guarda la fecha
        reloj.Advance(TimeSpan.FromHours(23));
        Assert.False(await servicio.ReiniciarSiCorrespondeAsync());
        Assert.Equal(1, await db.SugerenciaColeccion.CountDocumentsAsync(s => s.Sugerencia == "Del visitante"));

        reloj.Advance(TimeSpan.FromHours(2));
        Assert.True(await servicio.ReiniciarSiCorrespondeAsync());
        Assert.Equal(0, await db.SugerenciaColeccion.CountDocumentsAsync(s => s.Sugerencia == "Del visitante"));
        Assert.Equal(reloj.GetUtcNow().UtcDateTime, await DatosDemo.UltimoReinicioAsync(db));
    }
}
