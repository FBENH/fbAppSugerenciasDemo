

namespace SuegerenciasAppLibrary.DataAccess
{
    public class MongoUserData : IUserData
    {
        private readonly IMongoCollection<UsuarioModel> _usuarios;
        public MongoUserData(IDBConnection db)
        {
            _usuarios = db.UsuarioColeccion;
        }
        public async Task<List<UsuarioModel>> GetUsuariosAsync()
        {
            var resultado = await _usuarios.FindAsync(_ => true);
            return resultado.ToList();
        }
        public async Task<UsuarioModel> GetUsuario(string id)
        {
            var resultado = await _usuarios.FindAsync(u => u.Id == id);
            return resultado.FirstOrDefault();
        }
        public async Task<UsuarioModel> GetUsuarioFromAuthentication(string objectId)
        {
            var resultado = await _usuarios.FindAsync(u => u.ObjectIdentifier == objectId);
            return resultado.FirstOrDefault();
        }
        public Task CreateUsuario(UsuarioModel usuario)
        {
            return _usuarios.InsertOneAsync(usuario);
        }
        public Task UpdateUsuario(UsuarioModel usuario)
        {
            var filtro = Builders<UsuarioModel>.Filter.Eq("Id", usuario.Id);
            return _usuarios.ReplaceOneAsync(filtro, usuario, new ReplaceOptions { IsUpsert = true });
        }
    }
}
