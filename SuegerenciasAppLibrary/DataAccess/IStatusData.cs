namespace SuegerenciasAppLibrary.DataAccess
{
    public interface IStatusData
    {
        Task CreateEstados(EstadoModel estado);
        Task<List<EstadoModel>> GetAllEstados();
    }
}