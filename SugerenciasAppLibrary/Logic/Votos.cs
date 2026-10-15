namespace SugerenciasAppLibrary.Logic;

public enum EstadoVoto
{
    SinVotos,
    Votada,
    NoVotada
}

public static class Votos
{
    public static bool EsAutor(SugerenciaModel sugerencia, string usuarioId)
    {
        return usuarioId is not null && sugerencia.Autor?.Id == usuarioId;
    }

    /// <summary>
    /// Agrega el voto del usuario o lo quita si ya había votado.
    /// Devuelve null si el usuario es el autor (no puede votar su propia sugerencia),
    /// true si quedó votada y false si se quitó el voto.
    /// </summary>
    public static bool? Alternar(SugerenciaModel sugerencia, string usuarioId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usuarioId);

        if (EsAutor(sugerencia, usuarioId))
        {
            return null;
        }

        sugerencia.VotosDeUsuarios ??= new();
        if (sugerencia.VotosDeUsuarios.Add(usuarioId))
        {
            return true;
        }

        sugerencia.VotosDeUsuarios.Remove(usuarioId);
        return false;
    }

    public static string TextoSuperior(SugerenciaModel sugerencia, string usuarioId)
    {
        int votos = sugerencia.VotosDeUsuarios?.Count ?? 0;
        if (votos > 0)
        {
            return votos.ToString("00");
        }

        return EsAutor(sugerencia, usuarioId) ? "Esperando" : "Click para";
    }

    public static string TextoInferior(SugerenciaModel sugerencia, string usuarioId)
    {
        int votos = sugerencia.VotosDeUsuarios?.Count ?? 0;
        if (votos == 1)
        {
            return "Voto";
        }

        return votos > 0 || EsAutor(sugerencia, usuarioId) ? "Votos" : "Votar";
    }

    public static EstadoVoto Estado(SugerenciaModel sugerencia, string usuarioId)
    {
        if (sugerencia.VotosDeUsuarios is null || sugerencia.VotosDeUsuarios.Count == 0)
        {
            return EstadoVoto.SinVotos;
        }

        return usuarioId is not null && sugerencia.VotosDeUsuarios.Contains(usuarioId)
            ? EstadoVoto.Votada
            : EstadoVoto.NoVotada;
    }

    /// <summary>Sufijo CSS según el estado de la sugerencia (completado, en-revision, etc.).</summary>
    public static string SufijoEstadoCss(SugerenciaModel sugerencia)
    {
        return sugerencia?.SugerenciaEstado?.EstadoNombre switch
        {
            "Completado" => "completado",
            "En revision" => "en-revision",
            "Proximamente" => "proximamente",
            "Rechazada" => "rechazada",
            _ => "ninguno"
        };
    }
}
