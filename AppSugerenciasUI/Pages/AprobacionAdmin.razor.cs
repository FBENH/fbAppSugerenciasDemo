namespace AppSugerenciasUI.Pages;

public partial class AprobacionAdmin
{
    private List<SugerenciaModel> sugerencias;
    private SugerenciaModel editingModel;
    private string editandoTitulo = "";
    private string tituloEditado = "";
    private string editandoDescripcion = "";
    private string descripcionEditada = "";
    protected async override Task OnInitializedAsync()
    {
        sugerencias = await sugerenciaData.GetAllSugerenciasEsperandoAprobacion();
    }

    private async Task AprobarSugerencia(SugerenciaModel sugerencia)
    {
        sugerencia.AprobadoParaMostrar = true;
        sugerencias.Remove(sugerencia);
        await sugerenciaData.UpdateSugerencia(sugerencia);
    }

    private async Task RechazarSugerencia(SugerenciaModel sugerencia)
    {
        sugerencia.Rechazado = true;
        sugerencias.Remove(sugerencia);
        await sugerenciaData.UpdateSugerencia(sugerencia);
    }

    private void EditarTitulo(SugerenciaModel model)
    {
        editingModel = model;
        tituloEditado = model.Sugerencia;
        editandoTitulo = model.Id;
        editandoDescripcion = "";
    }

    private async Task GuardarTitulo(SugerenciaModel model)
    {
        editandoTitulo = string.Empty;
        model.Sugerencia = tituloEditado;
        await sugerenciaData.UpdateSugerencia(model);
    }

    private void EditarDescripcion(SugerenciaModel model)
    {
        editingModel = model;
        descripcionEditada = model.Descripcion;
        editandoTitulo = "";
        editandoDescripcion = model.Id;
    }

    private async Task GuardarDescripcion(SugerenciaModel model)
    {
        editandoDescripcion = string.Empty;
        model.Descripcion = descripcionEditada;
        await sugerenciaData.UpdateSugerencia(model);
    }

    private void ClosePage()
    {
        navManager.NavigateTo("/");
    }
}