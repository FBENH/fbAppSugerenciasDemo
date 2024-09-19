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
    private bool showCategorias = false;
    private bool showEstados = false;
    
    protected override async Task OnInitializedAsync()
    {
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

            if (nombre.Equals(loggedInUsuario.Nombre) == false)
            {
                isDirty = true;
                loggedInUsuario.Nombre = nombre;
            }

            if (apellido.Equals(loggedInUsuario.Apellido) == false)
            {
                isDirty = true;
                loggedInUsuario.Apellido = apellido;
            }

            if (displayName.Equals(loggedInUsuario.DisplayName) == false)
            {
                isDirty = true;
                loggedInUsuario.DisplayName = displayName;
            }

            if (email.Equals(loggedInUsuario.Email) == false)
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
        var output = await sugerenciaData.GetAllSugerenciasAprobadas();
        if (categoriaSeleccionada != "Todas")
        {
            output = output.Where(s => s.Categoria?.CategoriaNombre == categoriaSeleccionada).ToList();
        }

        if (estadoSeleccionado != "Todos")
        {
            output = output.Where(s => s.SugerenciaEstado?.EstadoNombre == estadoSeleccionado).ToList();
        }

        if (string.IsNullOrWhiteSpace(textoBusqueda) == false)
        {
            output = output.Where(s => s.Sugerencia.Contains(textoBusqueda, StringComparison.InvariantCultureIgnoreCase) || s.Descripcion.Contains(textoBusqueda, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        if (isSortedByNew)
        {
            output = output.OrderByDescending(s => s.FechaCreacion).ToList();
        }
        else
        {
            output = output.OrderByDescending(s => s.VotosDeUsuarios.Count).ThenByDescending(s => s.FechaCreacion).ToList();
        }

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
        showCategorias = false;
        await FilterSugerencias();
    }

    private async Task OnEstadoClick(string estado = "Todos")
    {
        estadoSeleccionado = estado;
        showEstados = false;
        await FilterSugerencias();
    }

    private async Task Votar(SugerenciaModel sugerencia)
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
            if (isSortedByNew == false)
            {
                sugerencias = sugerencias.OrderByDescending(s => s.VotosDeUsuarios.Count).ThenByDescending(s => s.FechaCreacion).ToList();
            }
        }
        else
        {
            navManager.NavigateTo("/MicrosoftIdentity/Account/SignIn", true);
        }
    }

    private string GetUpvoteTopText(SugerenciaModel sugerencia)
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

    private string GetUpvoteBottomText(SugerenciaModel sugerencia)
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

    private void OpenDetails(SugerenciaModel sugerencia)
    {
        navManager.NavigateTo($"/Detalles/{sugerencia.Id}");
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

    private string SortedByNewClass(bool IsNew)
    {
        if (IsNew == isSortedByNew)
        {
            return "sort-selected";
        }
        else
        {
            return "";
        }
    }

    private string GetVoteClass(SugerenciaModel sugerencia)
    {
        if (sugerencia.VotosDeUsuarios is null || sugerencia.VotosDeUsuarios.Count == 0)
        {
            return "sugerencia-entry-sin-votos";
        }
        else if (sugerencia.VotosDeUsuarios.Contains(loggedInUsuario?.Id))
        {
            return "sugerencia-entry-votada";
        }
        else
        {
            return "sugerencia-entry-no-votada";
        }
    }

    private string GetSugerenciaEstadoClass(SugerenciaModel sugerencia)
    {
        if (sugerencia is null || sugerencia.SugerenciaEstado is null)
        {
            return "sugerencia-entry-estado-ninguno";
        }

        string output = sugerencia.SugerenciaEstado.EstadoNombre switch
        {
            "Completado" => "sugerencia-entry-estado-completado",
            "En revision" => "sugerencia-entry-estado-en-revision",
            "Proximamente" => "sugerencia-entry-estado-proximamente",
            "Rechazada" => "sugerencia-entry-estado-rechazada",
            _ => "sugerencia-entry-estado-ninguno"
        };
        return output;
    }

    private string GetCategoriaSeleccionada(string categoria = "Todas")
    {
        if (categoria == categoriaSeleccionada)
        {
            return "categoria-seleccionada";
        }
        else
        {
            return "";
        }
    }

    private string GetEstadoSeleccionado(string estado = "Todos")
    {
        if (estado == estadoSeleccionado)
        {
            return "estado-seleccionado";
        }
        else
        {
            return "";
        }
    }

    private async Task ArchivarSugerencia()
    {
        archivandoSugerencia.Archivado = true;
        await sugerenciaData.UpdateSugerencia(archivandoSugerencia);
        sugerencias.Remove(archivandoSugerencia);
        archivandoSugerencia = null;
        //await FilterSugerencias();
    }
}
