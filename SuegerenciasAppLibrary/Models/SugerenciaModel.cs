namespace SuegerenciasAppLibrary.Models
{
    public class SugerenciaModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Sugerencia { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public CategoriaModel Categoria { get; set; }
        public UsuarioBasicoModel Autor { get; set; }
        public HashSet<string> VotosDeUsuarios { get; set; } = new();
        public EstadoModel SugerenciaEstado { get; set; }
        public string NotasAdmins { get; set; }
        public bool AprobadoParaMostrar { get; set; } = false;
        public bool Archivado { get; set; } = false;
        public bool Rechazado { get; set; } = false;

    }
}
