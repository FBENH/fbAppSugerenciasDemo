namespace AppSugerencias.Tests.Logic;

public class BusquedaParecidasTests
{
    [Theory]
    [InlineData("Curso de Docker para .NET", "Docker NET")]
    [InlineData("¿Cómo usar índices en MongoDB?", "usar índices MongoDB")]
    [InlineData("docker DOCKER", "docker")]
    public void ConsultaSacaPalabrasComunesYRepetidas(string texto, string esperado)
    {
        Assert.Equal(esperado, BusquedaParecidas.Consulta(texto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("curso de")]
    public void SinTerminosUtilesNoBusca(string texto)
    {
        Assert.Null(BusquedaParecidas.Consulta(texto));
    }
}
