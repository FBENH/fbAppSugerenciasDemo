namespace SuegerenciasAppLibrary.Models
{
    public class SugerenciaBasicaModel
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Sugerencia { get; set; }

        public SugerenciaBasicaModel()
        {

        }
        public SugerenciaBasicaModel(SugerenciaModel sugerencia)
        {
            Id = sugerencia.Id;
            Sugerencia = sugerencia.Sugerencia;
        }
    }
}
