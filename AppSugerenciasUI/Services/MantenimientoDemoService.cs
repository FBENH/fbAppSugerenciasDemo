using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AppSugerenciasUI.Services;

/// <summary>
/// Al arrancar crea los índices y carga datos de ejemplo si la base está vacía.
/// Si el reinicio automático está activado, revisa cada hora si pasó el intervalo desde el último
/// reinicio. Se guarda la fecha en la base en lugar de usar un horario fijo porque el App Service
/// puede estar dormido a esa hora; así el reinicio ocurre en cuanto la app vuelve a estar activa.
/// </summary>
public sealed class MantenimientoDemoService(
    IDBConnection db,
    IMemoryCache cache,
    NotificadorSugerencias notificador,
    IOptions<OpcionesDemo> opciones,
    TimeProvider reloj,
    ILogger<MantenimientoDemoService> logger) : BackgroundService
{
    private static readonly TimeSpan IntervaloRevision = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await BaseDeDatos.CrearIndicesAsync(db);
            if (opciones.Value.SembrarSiVacia && await DatosDemo.SembrarSiVaciaAsync(db))
            {
                logger.LogInformation("Base vacía: se cargaron los datos de ejemplo.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo preparar la base de datos al iniciar.");
        }

        if (opciones.Value.ReinicioAutomatico == false)
        {
            return;
        }

        using var timer = new PeriodicTimer(IntervaloRevision, reloj);
        do
        {
            try
            {
                await ReiniciarSiCorrespondeAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falló el reinicio de los datos del demo.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<bool> ReiniciarSiCorrespondeAsync()
    {
        DateTime ahora = reloj.GetUtcNow().UtcDateTime;
        DateTime? ultimo = await DatosDemo.UltimoReinicioAsync(db);
        if (ultimo is null)
        {
            // Primera vez: se toma como punto de partida sin borrar lo que haya.
            await DatosDemo.GuardarUltimoReinicioAsync(db, ahora);
            return false;
        }

        if (ahora - ultimo.Value < TimeSpan.FromHours(opciones.Value.HorasEntreReinicios))
        {
            return false;
        }

        await DatosDemo.ReiniciarAsync(db, ahora);
        (cache as MemoryCache)?.Clear();
        notificador.Notificar(null, TipoCambio.Reinicio);
        logger.LogInformation("Se reiniciaron los datos del demo.");
        return true;
    }
}
