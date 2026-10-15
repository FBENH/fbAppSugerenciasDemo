namespace AppSugerenciasUI.Pages;
public partial class Index
{
    private UsuarioModel loggedInUsuario;
    private List<SugerenciaModel> sugerencias;
    private List<CategoriaModel> categorias;
    private List<EstadoModel> estados;
    private SugerenciaModel archivandoSugerencia;
    private string categoriaSeleccionada = "Todas";
    private string estadoSeleccionado = "Todos";
    private string textoBusqueda = "";
    bool isSortedByNew = true;

    protected override async Task OnInitializedAsync()
    {
        notificador.Cambio += AlCambiarSugerencias;
        categorias = await categoryData.GetAllCategorias();
        estados = await statusData.GetAllEstados();
        await CargarVerificarUsuario();
    }

    private async Task CargarVerificarUsuario()
    {
        var authState = await authProvider.GetAuthenticationStateAsync();
        string objectId = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("objectidentifier"))?.Value;
        if (string.IsNullOrWhiteSpace(objectId) == false)
        {
            loggedInUsuario = await userData.GetUsuarioFromAuthentication(objectId) ?? new();
            string nombre = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("givenname"))?.Value;
            string apellido = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("surname"))?.Value;
            string displayName = authState.User.Claims.FirstOrDefault(c => c.Type.Equals("name"))?.Value;
            string email = authState.User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value;
            bool isDirty = false;
            if (objectId.Equals(loggedInUsuario.ObjectIdentifier) == false)
            {
                isDirty = true;
                loggedInUsuario.ObjectIdentifier = objectId;
            }

            if (string.Equals(nombre, loggedInUsuario.Nombre) == false)
            {
                isDirty = true;
                loggedInUsuario.Nombre = nombre;
            }

            if (string.Equals(apellido, loggedInUsuario.Apellido) == false)
            {
                isDirty = true;
                loggedInUsuario.Apellido = apellido;
            }

            if (string.Equals(displayName, loggedInUsuario.DisplayName) == false)
            {
                isDirty = true;
                loggedInUsuario.DisplayName = displayName;
            }

            if (string.Equals(email, loggedInUsuario.Email) == false)
            {
                isDirty = true;
                loggedInUsuario.Email = email;
            }

            if (isDirty)
            {
                if (string.IsNullOrWhiteSpace(loggedInUsuario.Id))
                {
                    await userData.CreateUsuario(loggedInUsuario);
                }
                else
                {
                    await userData.UpdateUsuario(loggedInUsuario);
                }
            }
        }
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadFilterEstado();
            await FilterSugerencias();
            StateHasChanged();
        }
    }

    private async Task LoadFilterEstado()
    {
        var stringResultados = await sessionStorage.GetAsync<string>(nameof(categoriaSeleccionada));
        categoriaSeleccionada = stringResultados.Success ? stringResultados.Value : "Todas";
        stringResultados = await sessionStorage.GetAsync<string>(nameof(estadoSeleccionado));
        estadoSeleccionado = stringResultados.Success ? stringResultados.Value : "Todos";
        stringResultados = await sessionStorage.GetAsync<string>(nameof(textoBusqueda));
        textoBusqueda = stringResultados.Success ? stringResultados.Value : "";
        var boolResultados = await sessionStorage.GetAsync<bool>(nameof(isSortedByNew));
        isSortedByNew = boolResultados.Success ? boolResultados.Value : true;
    }

    private async Task GuardarEstadoDeFiltros()
    {
        await sessionStorage.SetAsync(nameof(categoriaSeleccionada), categoriaSeleccionada);
        await sessionStorage.SetAsync(nameof(estadoSeleccionado), estadoSeleccionado);
        await sessionStorage.SetAsync(nameof(textoBusqueda), textoBusqueda);
        await sessionStorage.SetAsync(nameof(isSortedByNew), isSortedByNew);
    }

    private async Task FilterSugerencias()
    {
        var aprobadas = await sugerenciaData.GetAllSugerenciasAprobadas();
        var output = SugerenciaFiltros.Filtrar(aprobadas, categoriaSeleccionada, estadoSeleccionado, textoBusqueda, isSortedByNew);
        sugerencias = output;
        await GuardarEstadoDeFiltros();
    }

    private async Task OrderByNew(bool isNew)
    {
        isSortedByNew = isNew;
        await FilterSugerencias();
    }

    private async Task InputBusqueda(string inputBusqueda)
    {
        textoBusqueda = inputBusqueda;
        await FilterSugerencias();
    }

    private async Task OnCategoriaClick(string categoria = "Todas")
    {
        categoriaSeleccionada = categoria;
        await FilterSugerencias();
    }

    private async Task OnEstadoClick(string estado = "Todos")
    {
        estadoSeleccionado = estado;
        await FilterSugerencias();
    }

    private async Task Votar(SugerenciaModel sugerencia)
    {
        if (loggedInUsuario is not null)
        {
            if (Votos.Alternar(sugerencia, loggedInUsuario.Id) is null)
            {
                return;
            }

            await sugerenciaData.VotarSugerencia(sugerencia.Id, loggedInUsuario.Id);
            if (isSortedByNew == false)
            {
                sugerencias = SugerenciaFiltros.Ordenar(sugerencias, ordenarPorNuevas: false);
            }
        }
        else
        {
            navManager.NavigateTo("/MicrosoftIdentity/Account/SignIn", true);
        }
    }

    private void CargarPaginaCrear()
    {
        if (loggedInUsuario is not null)
        {
            navManager.NavigateTo("/Crear");
        }
        else
        {
            navManager.NavigateTo("/MicrosoftIdentity/Account/SignIn", true);
        }
    }

    private async Task ArchivarSugerencia()
    {
        archivandoSugerencia.Archivado = true;
        await sugerenciaData.UpdateSugerencia(archivandoSugerencia);
        sugerencias.Remove(archivandoSugerencia);
        archivandoSugerencia = null;
    }

    private async Task AlCambiarSugerencias(CambioSugerencia cambio)
    {
        await InvokeAsync(async () =>
        {
            if (sugerencias is null)
            {
                return;
            }

            if (cambio.Tipo == TipoCambio.Reinicio)
            {
                categorias = await categoryData.GetAllCategorias();
                estados = await statusData.GetAllEstados();
            }

            await FilterSugerencias();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        notificador.Cambio -= AlCambiarSugerencias;
    }
}
