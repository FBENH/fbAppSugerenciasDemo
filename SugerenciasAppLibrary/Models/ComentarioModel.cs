namespace SugerenciasAppLibrary.Models;

public class ComentarioModel
{
    public const int LargoMaximo = 1000;

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string SugerenciaId { get; set; }

    public UsuarioBasicoModel Autor { get; set; }
    public string Texto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public bool EsAdmin { get; set; }
}
