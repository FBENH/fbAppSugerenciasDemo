using AppSugerenciasUI.Models;
namespace AppSugerenciasUI.Pages;

public partial class Crear
{
    private CrearSugerenciaModel sugerencia = new();
    private List<CategoriaModel> categorias;
    private UsuarioModel loggedInUsuario;
    protected async override Task OnInitializedAsync()
    {
        categorias = await categoriaData.GetAllCategorias();
        loggedInUsuario = await authProvider.GetUsuarioFromAuth(userData);
    }

    private void ClosePage()
    {
        navManager.NavigateTo("/");
    }

    private async Task CrearSugerencia()
    {
        SugerenciaModel s = new();
        s.Sugerencia = sugerencia.Sugerencia;
        s.Descripcion = sugerencia.Descripcion;
        s.Autor = new UsuarioBasicoModel(loggedInUsuario);
        s.Categoria = categorias.Where(c => c.Id == sugerencia.CategoriaId).FirstOrDefault();
        if (s.Categoria is null)
        {
            sugerencia.CategoriaId = "";
            return;
        }

        await sugerenciaData.CreateSugerencia(s);
        sugerencia = new();
        ClosePage();
    }
}