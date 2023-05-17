namespace AppSugerenciasUI.Pages;

public partial class Perfil
{
    private UsuarioModel loggedInUsuario;
    private List<SugerenciaModel> sugerido;
    private List<SugerenciaModel> aprobadas;
    private List<SugerenciaModel> archivadas;
    private List<SugerenciaModel> pendientes;
    private List<SugerenciaModel> rechazadas;
    protected async override Task OnInitializedAsync()
    {
        //Reemplazar con busqueda de usuario
        loggedInUsuario = await authProvider.GetUsuarioFromAuth(userData);
        var resultados = await sugerenciaData.GetSugerenciasUsuario(loggedInUsuario.Id);
        if (loggedInUsuario is not null && resultados is not null)
        {
            sugerido = resultados.OrderByDescending(s => s.FechaCreacion).ToList();
            aprobadas = sugerido.Where(s => s.AprobadoParaMostrar && s.Archivado == false & s.Rechazado == false).ToList();
            archivadas = sugerido.Where(s => s.Archivado && s.Rechazado == false).ToList();
            pendientes = sugerido.Where(s => s.AprobadoParaMostrar == false && s.Rechazado == false).ToList();
            rechazadas = sugerido.Where(s => s.Rechazado).ToList();
        }
    }

    private void ClosePage()
    {
        navManager.NavigateTo("/");
    }
}