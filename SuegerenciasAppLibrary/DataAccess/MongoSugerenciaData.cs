

using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography.X509Certificates;
using System.Transactions;

namespace SuegerenciasAppLibrary.DataAccess
{
    public class MongoSugerenciaData : IMongoSugerenciaData
    {
        private readonly IDBConnection _db;
        private readonly IUserData _userData;
        private readonly IMemoryCache _cache;
        private readonly IMongoCollection<SugerenciaModel> _sugerencias;
        private const string CacheName = "SugerenciaData";

        public MongoSugerenciaData(IDBConnection db, IUserData userData, IMemoryCache cache)
        {
            this._db = db;
            this._userData = userData;
            this._cache = cache;
            _sugerencias = db.SugerenciaColeccion;
        }

        public async Task<List<SugerenciaModel>> GetAllSugerencias()
        {
            var output = _cache.Get<List<SugerenciaModel>>(CacheName);
            if (output is null)
            {
                var resultado = await _sugerencias.FindAsync(s => s.Archivado == false);
                output = resultado.ToList();

                _cache.Set(CacheName, output, TimeSpan.FromMinutes(1));
            }
            return output;
        }
        public async Task<List<SugerenciaModel>> GetSugerenciasUsuario(string usuarioId) 
        {
            var output = _cache.Get<List<SugerenciaModel>>(usuarioId);
            if(output is null) 
            {
                var resultados = await _sugerencias.FindAsync(s => s.Autor.Id == usuarioId);
                output = resultados.ToList();
                _cache.Set(usuarioId, output, TimeSpan.FromMinutes(1));
            }
            return output;
        }
        public async Task<List<SugerenciaModel>> GetAllSugerenciasAprobadas()
        {
            var output = await GetAllSugerencias();
            return output.Where(x => x.AprobadoParaMostrar).ToList();
        }
        public async Task<SugerenciaModel> GetSugerencia(string id)
        {
            var resultado = await _sugerencias.FindAsync(s => s.Id == id);
            return resultado.FirstOrDefault();
        }
        public async Task<List<SugerenciaModel>> GetAllSugerenciasEsperandoAprobacion()
        {
            var output = await GetAllSugerencias();
            return output.Where(x =>
                x.AprobadoParaMostrar == false
                && x.Rechazado == false).ToList();
        }
        public async Task UpdateSugerencia(SugerenciaModel sugerencia)
        {
            await _sugerencias.ReplaceOneAsync(s => s.Id == sugerencia.Id, sugerencia);
            _cache.Remove(CacheName);
        }
        public async Task VotarSugerencia(string SugerenciaId, string UsuarioId)
        {
            var client = _db.Client;

            using var session = await client.StartSessionAsync();
            session.StartTransaction();
            try
            {
                var db = client.GetDatabase(_db.DbName);
                var sugerenciaEnTransaccion = db.GetCollection<SugerenciaModel>(_db.SugerenciaNombreColeccion);
                var sugerencia = (await sugerenciaEnTransaccion.FindAsync(s => s.Id == SugerenciaId)).First();

                bool Votado = sugerencia.VotosDeUsuarios.Add(UsuarioId);
                if (Votado == false)
                {
                    sugerencia.VotosDeUsuarios.Remove(UsuarioId);
                }

                await sugerenciaEnTransaccion.ReplaceOneAsync(session, s => s.Id == SugerenciaId, sugerencia);

                var UsuariosEnTransaccion = db.GetCollection<UsuarioModel>(_db.UsuarioNombreColeccion);
                var usuario = await _userData.GetUsuario(UsuarioId);

                if (Votado)
                {
                    usuario.SugerenciasVotadas.Add(new SugerenciaBasicaModel(sugerencia));
                }
                else
                {
                    var sugerenciaParaRemover = usuario.SugerenciasVotadas.Where(s => s.Id == SugerenciaId).First();
                    usuario.SugerenciasVotadas.Remove(sugerenciaParaRemover);
                }
                await UsuariosEnTransaccion.ReplaceOneAsync(session, u => u.Id == UsuarioId, usuario);
                await session.CommitTransactionAsync();

                _cache.Remove(CacheName);
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                throw;
            }

        }
        public async Task CreateSugerencia(SugerenciaModel sugerencia)
        {
            var client = _db.Client;
            using var session = await client.StartSessionAsync();

            session.StartTransaction();

            try
            {
                var db = client.GetDatabase(_db.DbName);
                var sugerenciaEnTransaccion = db.GetCollection<SugerenciaModel>(_db.SugerenciaNombreColeccion);
                await sugerenciaEnTransaccion.InsertOneAsync(session,sugerencia);

                var usuariosEnTransaccion = db.GetCollection<UsuarioModel>(_db.UsuarioNombreColeccion);
                var usuario = await _userData.GetUsuario(sugerencia.Autor.Id);
                usuario.SugerenciasCreadas.Add(new SugerenciaBasicaModel(sugerencia));
                await usuariosEnTransaccion.ReplaceOneAsync(session,u => u.Id == usuario.Id, usuario);

                await session.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                throw;
            }
        }
    }
}
