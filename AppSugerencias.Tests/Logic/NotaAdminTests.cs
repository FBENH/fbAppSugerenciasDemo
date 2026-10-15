namespace AppSugerencias.Tests.Logic;

public class NotaAdminTests
{
    [Theory]
    [InlineData("https://youtube.com/watch?v=abc", "https://youtube.com/watch?v=abc")]
    [InlineData("  http://ejemplo.com/curso  ", "http://ejemplo.com/curso")]
    public void TryCrearUrlRecurso_AceptaHttpYHttps(string input, string esperada)
    {
        Assert.True(NotaAdmin.TryCrearUrlRecurso(input, out string url));
        Assert.Equal(esperada, url);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("ejemplo.com")]
    [InlineData("/ruta/relativa")]
    [InlineData("")]
    [InlineData(null)]
    public void TryCrearUrlRecurso_RechazaOtrosEsquemasYUrlsRelativas(string input)
    {
        Assert.False(NotaAdmin.TryCrearUrlRecurso(input, out string url));
        Assert.Null(url);
    }

    [Fact]
    public void Leer_UsaUrlRecursoCuandoExiste()
    {
        var sugerencia = new SugerenciaModel
        {
            NotasAdmins = NotaAdmin.TextoCompletado,
            UrlRecurso = "https://ejemplo.com/curso"
        };

        var (texto, url) = NotaAdmin.Leer(sugerencia);

        Assert.Equal(NotaAdmin.TextoCompletado, texto);
        Assert.Equal("https://ejemplo.com/curso", url);
    }

    [Fact]
    public void Leer_SeparaElLinkDeNotasGuardadasComoHtml()
    {
        // Formato que guardaba la versión anterior (con la comilla de apertura faltante).
        var sugerencia = new SugerenciaModel
        {
            NotasAdmins = "Creamos un recurso sobre ese tema aquí: <a href=https://ejemplo.com/curso' target='_blank'>https://ejemplo.com/curso</a>"
        };

        var (texto, url) = NotaAdmin.Leer(sugerencia);

        Assert.Equal("Creamos un recurso sobre ese tema aquí:", texto);
        Assert.Equal("https://ejemplo.com/curso", url);
    }

    [Fact]
    public void Leer_DescartaLinksLegadosConEsquemaPeligroso()
    {
        var sugerencia = new SugerenciaModel
        {
            NotasAdmins = "Creamos un recurso sobre ese tema aquí: <a href=javascript:alert(1)' target='_blank'>x</a>"
        };

        var (texto, url) = NotaAdmin.Leer(sugerencia);

        Assert.Equal("Creamos un recurso sobre ese tema aquí:", texto);
        Assert.Null(url);
    }

    [Fact]
    public void Leer_NotasSinLink()
    {
        var (texto, url) = NotaAdmin.Leer(new SugerenciaModel { NotasAdmins = "Estamos evaluando esta sugerencia." });

        Assert.Equal("Estamos evaluando esta sugerencia.", texto);
        Assert.Null(url);
    }
}
