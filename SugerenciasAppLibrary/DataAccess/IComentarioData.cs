namespace SugerenciasAppLibrary.DataAccess;

public interface IComentarioData
{
    Task<List<ComentarioModel>> GetComentarios(string sugerenciaId);
    Task CrearComentario(ComentarioModel comentario);
    Task EliminarComentario(string id);
}
