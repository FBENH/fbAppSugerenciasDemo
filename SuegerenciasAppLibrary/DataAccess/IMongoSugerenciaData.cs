namespace SuegerenciasAppLibrary.DataAccess
{
    public interface IMongoSugerenciaData
    {
        Task CreateSugerencia(SugerenciaModel sugerencia);
        Task<List<SugerenciaModel>> GetAllSugerencias();
        Task<List<SugerenciaModel>> GetAllSugerenciasAprobadas();
        Task<List<SugerenciaModel>> GetAllSugerenciasEsperandoAprobacion();
        Task<SugerenciaModel> GetSugerencia(string id);
        Task<List<SugerenciaModel>> GetSugerenciasUsuario(string usuarioId);
        Task UpdateSugerencia(SugerenciaModel sugerencia);
        Task VotarSugerencia(string SugerenciaId, string UsuarioId);
    }
}