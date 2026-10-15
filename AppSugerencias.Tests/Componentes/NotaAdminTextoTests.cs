using AppSugerenciasUI.Shared;
using Bunit;

namespace AppSugerencias.Tests.Componentes;

public class NotaAdminTextoTests : BunitContext
{
    [Fact]
    public void MuestraElLinkDelRecurso()
    {
        var sugerencia = new SugerenciaModel
        {
            NotasAdmins = NotaAdmin.TextoCompletado,
            UrlRecurso = "https://ejemplo.com/curso"
        };

        var cut = Render<NotaAdminTexto>(p => p.Add(c => c.Sugerencia, sugerencia));

        var link = cut.Find("a");
        Assert.Equal("https://ejemplo.com/curso", link.GetAttribute("href"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
    }

    [Fact]
    public void NoRenderizaHtmlEscritoEnLasNotas()
    {
        var sugerencia = new SugerenciaModel
        {
            NotasAdmins = "Nota <img src=x onerror=alert(1)>"
        };

        var cut = Render<NotaAdminTexto>(p => p.Add(c => c.Sugerencia, sugerencia));

        Assert.Empty(cut.FindAll("img"));
        Assert.Contains("&lt;img", cut.Markup);
    }
}
