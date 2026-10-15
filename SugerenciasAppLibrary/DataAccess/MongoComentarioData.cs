namespace SugerenciasAppLibrary.DataAccess;

public class MongoComentarioData : IComentarioData
{
    private readonly IMongoCollection<ComentarioModel> _comentarios;
    private readonly NotificadorSugerencias _notificador;

    public MongoComentarioData(IDBConnection db, NotificadorSugerencias notificador)
    {
        _comentarios = db.ComentarioColeccion;
        _notificador = notificador;
    }

    public async Task<List<ComentarioModel>> GetComentarios(string sugerenciaId)
    {
        return await _comentarios.Find(c => c.SugerenciaId == sugerenciaId)
            .SortBy(c => c.Fecha)
            .ToListAsync();
    }

    public async Task CrearComentario(ComentarioModel comentario)
    {
        string texto = comentario.Texto?.Trim();
        if (string.IsNullOrEmpty(texto))
        {
            throw new ArgumentException("El comentario no puede estar vacío.", nameof(comentario));
        }

        comentario.Texto = texto.Length > ComentarioModel.LargoMaximo ? texto[..ComentarioModel.LargoMaximo] : texto;
        await _comentarios.InsertOneAsync(comentario);
        _notificador.Notificar(comentario.SugerenciaId, TipoCambio.Comentario);
    }

    public async Task EliminarComentario(string id)
    {
        var eliminado = await _comentarios.FindOneAndDeleteAsync(c => c.Id == id);
        if (eliminado is not null)
        {
            _notificador.Notificar(eliminado.SugerenciaId, TipoCambio.Comentario);
        }
    }
}
