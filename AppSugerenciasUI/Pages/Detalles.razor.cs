using Microsoft.AspNetCore.Components;
namespace AppSugerenciasUI.Pages;

public partial class Detalles
{
    [Parameter]
    public string Id { get; set; }

    private SugerenciaModel sugerencia;
    private UsuarioModel loggedInUsuario;
    private List<EstadoModel> estados;
    private string settingEstado = "";
    private string urlText = "";
    private bool urlInvalida = false;
    protected async override Task OnInitializedAsync()
    {
        sugerencia = await sugerenciaData.GetSugerencia(Id);
        loggedInUsuario = await authProvider.GetUsuarioFromAuth(userData);
        estados = await estadoData.GetAllEstados();
    }

    private async Task CompletarSetEstado()
    {
        string notas;
        string url = null;
        switch (settingEstado)
        {
            case "completado":
                if (NotaAdmin.TryCrearUrlRecurso(urlText, out url) == false)
                {
                    urlInvalida = true;
                    return;
                }

                notas = NotaAdmin.TextoCompletado;
                break;
            case "en revision":
                notas = "Estamos evaluando esta sugerencia.";
                break;
            case "proximamente":
                notas = "Buena sugerencia, estamos creando recursos para abordar este tema.";
                break;
            case "rechazada":
                notas = "Hemos decidido no implementar tu sugerencia en este momento.";
                break;
            default:
                return;
        }

        var estado = estados.FirstOrDefault(s => string.Equals(s.EstadoNombre, settingEstado, StringComparison.OrdinalIgnoreCase));
        if (estado is null)
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

    private void ClosePage()
    {
        navManager.NavigateTo("/");
    }

    private string GetUpvoteTopText()
    {
        return Votos.TextoSuperior(sugerencia, loggedInUsuario?.Id);
    }

    private string GetUpvoteBottomText()
    {
        return Votos.TextoInferior(sugerencia, loggedInUsuario?.Id);
    }

    private async Task Votar()
    {
        if (loggedInUsuario is not null)
        {
            if (Votos.Alternar(sugerencia, loggedInUsuario.Id) is null)
            {
                return;
            }

            await sugerenciaData.VotarSugerencia(sugerencia.Id, loggedInUsuario.Id);
        }
        else
        {
            navManager.NavigateTo("/MicrosoftIdentity/Account/SignIn", true);
        }
    }

    private string GetVoteClass()
    {
        return Votos.Estado(sugerencia, loggedInUsuario?.Id) switch
        {
            EstadoVoto.SinVotos => "sugerencia-detalle-sin-votos",
            EstadoVoto.Votada => "sugerencia-detalle-votada",
            _ => "sugerencia-detalle-no-votada"
        };
    }

    private string GetEstadoClass()
    {
        return $"sugerencia-detalle-estado-{Votos.SufijoEstadoCss(sugerencia)}";
    }
}
