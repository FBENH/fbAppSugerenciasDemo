using AppSugerenciasUI.Models;
using Microsoft.AspNetCore.Components;
namespace AppSugerenciasUI.Pages;

public partial class Crear
{
    private static readonly TimeSpan EsperaBusqueda = TimeSpan.FromMilliseconds(350);

    private CrearSugerenciaModel sugerencia = new();
    private List<CategoriaModel> categorias;
    private UsuarioModel loggedInUsuario;
    private List<SugerenciaModel> parecidas = [];
    private CancellationTokenSource busqueda;
    private int largoTitulo;
    private bool enviando;

    protected async override Task OnInitializedAsync()
    {
        categorias = await categoriaData.GetAllCategorias();
        loggedInUsuario = await authProvider.GetUsuarioFromAuth(userData);
    }

    private async Task AlEscribirTitulo(ChangeEventArgs e)
    {
        string texto = e.Value?.ToString() ?? "";
        largoTitulo = texto.Length;

        busqueda?.Cancel();
        busqueda = new CancellationTokenSource();
        var token = busqueda.Token;

        try
        {
            // Espera a que el usuario deje de escribir para no consultar la base en cada tecla.
            await Task.Delay(EsperaBusqueda, token);
            var resultado = await sugerenciaData.BuscarParecidas(texto);
            if (token.IsCancellationRequested == false)
            {
                parecidas = resultado;
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task CrearSugerencia()
    {
        var categoria = categorias.FirstOrDefault(c => c.Id == sugerencia.CategoriaId);
        if (categoria is null || loggedInUsuario is null)
        {
            sugerencia.CategoriaId = "";
            return;
        }

        enviando = true;
        SugerenciaModel s = new()
        {
            Sugerencia = sugerencia.Sugerencia.Trim(),
            Descripcion = sugerencia.Descripcion?.Trim(),
            Autor = new UsuarioBasicoModel(loggedInUsuario),
            Categoria = categoria,
        };

        await sugerenciaData.CreateSugerencia(s);
        navManager.NavigateTo("/Perfil");
    }

    public void Dispose()
    {
        busqueda?.Cancel();
        busqueda?.Dispose();
    }
}
