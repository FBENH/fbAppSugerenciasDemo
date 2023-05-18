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
    protected async override Task OnInitializedAsync()
    {
        sugerencia = await sugerenciaData.GetSugerencia(Id);
        loggedInUsuario = await authProvider.GetUsuarioFromAuth(userData);
        estados = await estadoData.GetAllEstados();
    }

    private async Task CompletarSetEstado()
    {
        switch (settingEstado)
        {
            case "completado":
                if (string.IsNullOrWhiteSpace(urlText))
                {
                    return;
                }

                sugerencia.SugerenciaEstado = estados.Where(s => s.EstadoNombre.ToLower() == settingEstado.ToLower()).First();
                sugerencia.NotasAdmins = $"Creamos un recurso sobre ese tema aqui: <a href={urlText}' target='_blank'>{urlText}</a>";
                break;
            case "en revision":
                sugerencia.SugerenciaEstado = estados.Where(s => s.EstadoNombre.ToLower() == settingEstado.ToLower()).First();
                sugerencia.NotasAdmins = "Estamos evaluando esta sugerencia.";
                break;
            case "proximamente":
                sugerencia.SugerenciaEstado = estados.Where(s => s.EstadoNombre.ToLower() == settingEstado.ToLower()).First();
                sugerencia.NotasAdmins = "Buena sugerencia,estamos creando recursos para abordar este tema.";
                break;
            case "rechazada":
                sugerencia.SugerenciaEstado = estados.Where(s => s.EstadoNombre.ToLower() == settingEstado.ToLower()).First();
                sugerencia.NotasAdmins = "Hemos decidido no implementar tu sugerencia en este momento.";
                break;
            default:
                return;
        }

        settingEstado = null;
        await sugerenciaData.UpdateSugerencia(sugerencia);
    }

    private void ClosePage()
    {
        navManager.NavigateTo("/");
    }

    private string GetUpvoteTopText()
    {
        if (sugerencia.VotosDeUsuarios?.Count > 0)
        {
            return sugerencia.VotosDeUsuarios.Count.ToString("00");
        }
        else
        {
            if (sugerencia.Autor.Id == loggedInUsuario?.Id)
            {
                return "Esperando";
            }
            else
            {
                return "Click para";
            }
        }
    }

    private string GetUpvoteBottomText()
    {
        if (sugerencia.VotosDeUsuarios?.Count == 1)
        {
            return "Voto";
        }

        if (sugerencia.VotosDeUsuarios?.Count > 0 || sugerencia.Autor.Id == loggedInUsuario?.Id)
        {
            return "Votos";
        }
        else
        {
            return "Votar";
        }
    }

    private async Task Votar()
    {
        if (loggedInUsuario is not null)
        {
            if (sugerencia.Autor.Id == loggedInUsuario.Id)
            {
                //No se puede votar en tu propia sugerencia
                return;
            }

            if (sugerencia.VotosDeUsuarios.Add(loggedInUsuario.Id) == false)
            {
                sugerencia.VotosDeUsuarios.Remove(loggedInUsuario.Id);
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
        if (sugerencia.VotosDeUsuarios is null || sugerencia.VotosDeUsuarios.Count == 0)
        {
            return "sugerencia-detalle-sin-votos";
        }
        else if (sugerencia.VotosDeUsuarios.Contains(loggedInUsuario?.Id))
        {
            return "sugerencia-detalle-votada";
        }
        else
        {
            return "sugerencia-detalle-no-votada";
        }
    }

    private string GetEstadoClass()
    {
        if (sugerencia is null || sugerencia.SugerenciaEstado is null)
        {
            return "sugerencia-detalle-estado-ninguno";
        }

        string output = sugerencia.SugerenciaEstado.EstadoNombre switch
        {
            "Completado" => "sugerencia-detalle-estado-completado",
            "En revisión" => "sugerencia-detalle-estado-en-revision",
            "Proximamente" => "sugerencia-detalle-estado-proximamente",
            "Rechazada" => "sugerencia-detalle-estado-rechazada",
            _ => "sugerencia-detalle-estado-ninguno"
        };
        return output;
    }
}
