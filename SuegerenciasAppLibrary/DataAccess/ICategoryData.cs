namespace SuegerenciasAppLibrary.DataAccess
{
    public interface ICategoryData
    {
        Task CreateCategoria(CategoriaModel categoria);
        Task<List<CategoriaModel>> GetAllCategorias();
    }
}