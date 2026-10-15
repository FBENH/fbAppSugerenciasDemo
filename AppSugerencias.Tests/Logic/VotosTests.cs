namespace AppSugerencias.Tests.Logic;

public class VotosTests
{
    private static SugerenciaModel Sugerencia(string autorId = "autor", params string[] votos)
    {
        return new SugerenciaModel
        {
            Autor = new UsuarioBasicoModel { Id = autorId },
            VotosDeUsuarios = new HashSet<string>(votos)
        };
    }

    [Fact]
    public void Alternar_AgregaElVotoSiNoHabiaVotado()
    {
        var sugerencia = Sugerencia();

        bool? resultado = Votos.Alternar(sugerencia, "u1");

        Assert.True(resultado);
        Assert.Contains("u1", sugerencia.VotosDeUsuarios);
    }

    [Fact]
    public void Alternar_QuitaElVotoSiYaHabiaVotado()
    {
        var sugerencia = Sugerencia("autor", "u1");

        bool? resultado = Votos.Alternar(sugerencia, "u1");

        Assert.False(resultado);
        Assert.Empty(sugerencia.VotosDeUsuarios);
    }

    [Fact]
    public void Alternar_ElAutorNoPuedeVotarSuPropiaSugerencia()
    {
        var sugerencia = Sugerencia("autor");

        bool? resultado = Votos.Alternar(sugerencia, "autor");

        Assert.Null(resultado);
        Assert.Empty(sugerencia.VotosDeUsuarios);
    }

    [Theory]
    [InlineData(0, "u1", "Click para", "Votar")]
    [InlineData(0, "autor", "Esperando", "Votos")]
    [InlineData(0, null, "Click para", "Votar")]
    [InlineData(1, "u1", "01", "Voto")]
    [InlineData(12, "u1", "12", "Votos")]
    public void Textos_SegunCantidadDeVotosYUsuario(int cantidad, string usuarioId, string superior, string inferior)
    {
        var votos = Enumerable.Range(0, cantidad).Select(i => $"v{i}").ToArray();
        var sugerencia = Sugerencia("autor", votos);

        Assert.Equal(superior, Votos.TextoSuperior(sugerencia, usuarioId));
        Assert.Equal(inferior, Votos.TextoInferior(sugerencia, usuarioId));
    }

    [Fact]
    public void Estado_DistingueSinVotosVotadaYNoVotada()
    {
        Assert.Equal(EstadoVoto.SinVotos, Votos.Estado(Sugerencia(), "u1"));
        Assert.Equal(EstadoVoto.Votada, Votos.Estado(Sugerencia("autor", "u1"), "u1"));
        Assert.Equal(EstadoVoto.NoVotada, Votos.Estado(Sugerencia("autor", "u2"), "u1"));
        Assert.Equal(EstadoVoto.NoVotada, Votos.Estado(Sugerencia("autor", "u2"), null));
    }

    [Theory]
    [InlineData("Completado", "completado")]
    [InlineData("En revision", "en-revision")]
    [InlineData("Proximamente", "proximamente")]
    [InlineData("Rechazada", "rechazada")]
    [InlineData("Otro", "ninguno")]
    public void SufijoEstadoCss_MapeaCadaEstado(string estado, string esperado)
    {
        var sugerencia = new SugerenciaModel { SugerenciaEstado = new EstadoModel { EstadoNombre = estado } };

        Assert.Equal(esperado, Votos.SufijoEstadoCss(sugerencia));
    }

    [Fact]
    public void SufijoEstadoCss_SinEstado()
    {
        Assert.Equal("ninguno", Votos.SufijoEstadoCss(new SugerenciaModel()));
        Assert.Equal("ninguno", Votos.SufijoEstadoCss(null));
    }
}
