namespace SugerenciasAppLibrary.Logic;

public enum TipoCambio
{
    Creada,
    Actualizada,
    Voto,
    Comentario,
    Reinicio
}

public sealed record CambioSugerencia(string SugerenciaId, TipoCambio Tipo);

/// <summary>
/// Avisa a todas las pantallas abiertas que una sugerencia cambió, para que se actualicen sin recargar.
/// Blazor Server mantiene todos los circuitos en el mismo proceso, así que alcanza con un evento en memoria.
/// Si la app corriera en varias instancias, esto tendría que pasar por un backplane (por ejemplo Redis).
/// </summary>
public sealed class NotificadorSugerencias
{
    public event Func<CambioSugerencia, Task> Cambio;

    public void Notificar(string sugerenciaId, TipoCambio tipo)
    {
        var handlers = Cambio;
        if (handlers is null)
        {
            return;
        }

        var cambio = new CambioSugerencia(sugerenciaId, tipo);
        foreach (Func<CambioSugerencia, Task> handler in handlers.GetInvocationList().Cast<Func<CambioSugerencia, Task>>())
        {
            // Cada pantalla se actualiza en su propio circuito; una que falle no frena a las demás.
            _ = EjecutarSinPropagar(handler, cambio);
        }
    }

    private static async Task EjecutarSinPropagar(Func<CambioSugerencia, Task> handler, CambioSugerencia cambio)
    {
        try
        {
            await handler(cambio);
        }
        catch
        {
            // El circuito del suscriptor pudo haberse cerrado; no es un error del que notifica.
        }
    }
}
