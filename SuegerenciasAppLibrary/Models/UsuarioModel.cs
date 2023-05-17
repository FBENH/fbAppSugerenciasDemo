namespace SuegerenciasAppLibrary.Models
{
    public class UsuarioModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string ObjectIdentifier { get; set; } //Para el Id que viene de Azure
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public List<SugerenciaBasicaModel> SugerenciasCreadas { get; set; } = new();
        public List<SugerenciaBasicaModel> SugerenciasVotadas { get; set; } = new();

    }
}
