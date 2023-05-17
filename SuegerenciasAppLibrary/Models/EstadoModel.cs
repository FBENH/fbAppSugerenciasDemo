namespace SuegerenciasAppLibrary.Models
{
    public class EstadoModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string EstadoNombre { get; set; }
        public string EstadoDescripcion { get; set; }
    }
}
