namespace AppSugerencias.Tests.Logic;

public class NotificadorSugerenciasTests
{
    [Fact]
    public async Task UnSuscriptorQueFallaNoFrenaALosDemas()
    {
        var notificador = new NotificadorSugerencias();
        var recibido = new TaskCompletionSource<CambioSugerencia>();
        notificador.Cambio += _ => throw new InvalidOperationException("circuito cerrado");
        notificador.Cambio += c => { recibido.SetResult(c); return Task.CompletedTask; };

        notificador.Notificar("abc", TipoCambio.Voto);

        Assert.Equal(new CambioSugerencia("abc", TipoCambio.Voto), await recibido.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void SinSuscriptoresNoHaceNada()
    {
        new NotificadorSugerencias().Notificar("abc", TipoCambio.Voto);
    }
}
