namespace SuegerenciasAppLibrary.Models
{
    public class UsuarioBasicoModel
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string DisplayName { get; set; }

        public UsuarioBasicoModel()
        {

        }

        public UsuarioBasicoModel(UsuarioModel usuario)
        {
            Id = usuario.Id;
            DisplayName = usuario.DisplayName;
        }
    }
}
