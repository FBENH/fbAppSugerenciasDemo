using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
namespace AppSugerenciasUI.Pages;

public partial class Detalles
{
    private static readonly (string Clave, string Texto)[] OpcionesEstado =
    [
        ("completado", "Completado"),
        ("en revision", "En revisión"),
        ("proximamente", "Próximamente"),
        ("rechazada", "Rechazada"),
    ];

    [Parameter]
    public string Id { get; set; }

    private bool cargando = true;
    private SugerenciaModel sugerencia;
    private UsuarioModel loggedInUsuario;
    private bool esAdmin;
    private List<EstadoModel> estados;
    private List<ComentarioModel> comentarios;
    private string settingEstado = "";
    private string urlText = "";
    private bool urlInvalida = false;
    private string nuevoComentario = "";
    private bool enviandoComentario;
    private bool votando;

    protected async override Task OnInitializedAsync()
    {
        var authState = await authProvider.GetAuthenticationStateAsync();
        esAdmin = (await authorizationService.AuthorizeAsync(authState.User, "Admin")).Succeeded;
        loggedInUsuario = await authProvider.GetUsuarioFromAuth(userData);
        estados = await estadoData.GetAllEstados();
        await CargarSugerencia();
        cargando = false;
        notificador.Cambio += AlCambiar;
    }

    private async Task CargarSugerencia()
    {
        sugerencia = MongoDB.Bson.ObjectId.TryParse(Id, out _) ? await sugerenciaData.GetSugerencia(Id) : null;

        // Las sugerencias sin aprobar o archivadas solo las ven el autor y los admins.
        bool visible = sugerencia is { AprobadoParaMostrar: true, Archivado: false }
            || esAdmin
            || (sugerencia is not null && Votos.EsAutor(sugerencia, loggedInUsuario?.Id));
        if (visible == false)
        {
            sugerencia = null;
            comentarios = null;
            return;
        }

        comentarios = await comentarioData.GetComentarios(sugerencia.Id);
    }

    private async Task AlCambiar(CambioSugerencia cambio)
    {
        if (cambio.Tipo != TipoCambio.Reinicio && cambio.SugerenciaId != Id)
        {
            return;
        }

        await InvokeAsync(async () =>
        {
            await CargarSugerencia();
            StateHasChanged();
        });
    }

    private async Task CompletarSetEstado()
    {
        string url = null;
        if (settingEstado == "completado" && NotaAdmin.TryCrearUrlRecurso(urlText, out url) == false)
        {
            urlInvalida = true;
            return;
        }

        string notas = NotaAdmin.TextoParaEstado(settingEstado);
        var estado = estados.FirstOrDefault(s => string.Equals(s.EstadoNombre, settingEstado, StringComparison.OrdinalIgnoreCase));
        if (notas is null || estado is null)
        {
            return;
        }

        sugerencia.SugerenciaEstado = estado;
        sugerencia.NotasAdmins = notas;
        sugerencia.UrlRecurso = url;
        settingEstado = null;
        urlText = "";
        urlInvalida = false;
        await sugerenciaData.UpdateSugerencia(sugerencia);
    }

    private async Task Votar()
    {
        if (loggedInUsuario is null)
        {
            navManager.NavigateTo("/MicrosoftIdentity/Account/SignIn", true);
            return;
        }

        // Mientras un voto se guarda, los clics siguientes se ignoran.
        if (votando || Votos.Alternar(sugerencia, loggedInUsuario.Id) is null)
        {
            return;
        }

        votando = true;
        try
        {
            await sugerenciaData.VotarSugerencia(sugerencia.Id, loggedInUsuario.Id);
        }
        catch (Exception ex)
        {
            // Se deshace el cambio en pantalla; el voto no se guardó.
            Votos.Alternar(sugerencia, loggedInUsuario.Id);
            logger.LogWarning(ex, "No se pudo guardar el voto en {SugerenciaId}", sugerencia.Id);
        }
        finally
        {
            votando = false;
        }
    }

    private async Task CrearComentario()
    {
        if (loggedInUsuario is null || string.IsNullOrWhiteSpace(nuevoComentario))
        {
            return;
        }

        enviandoComentario = true;
        try
        {
            await comentarioData.CrearComentario(new ComentarioModel
            {
                SugerenciaId = sugerencia.Id,
                Autor = new UsuarioBasicoModel(loggedInUsuario),
                Texto = nuevoComentario,
                EsAdmin = esAdmin,
            });
            nuevoComentario = "";
            comentarios = await comentarioData.GetComentarios(sugerencia.Id);
        }
        finally
        {
            enviandoComentario = false;
        }
    }

    private async Task EliminarComentario(ComentarioModel comentario)
    {
        if (esAdmin == false)
        {
            return;
        }

        await comentarioData.EliminarComentario(comentario.Id);
        comentarios.Remove(comentario);
    }

    public void Dispose()
    {
        notificador.Cambio -= AlCambiar;
    }
}
