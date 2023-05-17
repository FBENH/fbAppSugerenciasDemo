using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SuegerenciasAppLibrary.Models;

namespace SuegerenciasAppLibrary.DataAccess
{
    public class DBConnection : IDBConnection
    {
        private readonly IConfiguration _config;
        private readonly IMongoDatabase _db;
        private string _connectionId = "MongoDB";
        public string DbName { get; private set; }
        public string CategoriaNombreColeccion { get; private set; } = "categorias";
        public string EstadoNombreColeccion { get; private set; } = "estados";
        public string UsuarioNombreColeccion { get; private set; } = "usuarios";
        public string SugerenciaNombreColeccion { get; private set; } = "sugerencias";

        public MongoClient Client { get; private set; }
        public IMongoCollection<CategoriaModel> CategoriaColeccion { get; private set; }
        public IMongoCollection<EstadoModel> EstadoColeccion { get; private set; }
        public IMongoCollection<UsuarioModel> UsuarioColeccion { get; private set; }
        public IMongoCollection<SugerenciaModel> SugerenciaColeccion { get; private set; }
        public DBConnection(IConfiguration config)
        {
            _config = config;
            Client = new MongoClient(_config.GetConnectionString(_connectionId));
            DbName = config[key: "DatabaseName"];
            _db = Client.GetDatabase(DbName);

            CategoriaColeccion = _db.GetCollection<CategoriaModel>(CategoriaNombreColeccion);
            EstadoColeccion = _db.GetCollection<EstadoModel>(EstadoNombreColeccion);
            UsuarioColeccion = _db.GetCollection<UsuarioModel>(UsuarioNombreColeccion);
            SugerenciaColeccion = _db.GetCollection<SugerenciaModel>(SugerenciaNombreColeccion);
        }
    }
}
