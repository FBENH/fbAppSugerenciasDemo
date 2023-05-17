using MongoDB.Driver;

namespace SuegerenciasAppLibrary.DataAccess
{
    public interface IDBConnection
    {
        IMongoCollection<CategoriaModel> CategoriaColeccion { get; }
        string CategoriaNombreColeccion { get; }
        MongoClient Client { get; }
        string DbName { get; }
        IMongoCollection<EstadoModel> EstadoColeccion { get; }
        string EstadoNombreColeccion { get; }
        IMongoCollection<SugerenciaModel> SugerenciaColeccion { get; }
        string SugerenciaNombreColeccion { get; }
        IMongoCollection<UsuarioModel> UsuarioColeccion { get; }
        string UsuarioNombreColeccion { get; }
    }
}