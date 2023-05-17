namespace SuegerenciasAppLibrary.Models
{
    public class CategoriaModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string CategoriaNombre { get; set; }
        public string CategoriaDescripcion { get; set; }
    }
}
