using Microsoft.Extensions.Caching.Memory;

namespace SugerenciasAppLibrary.DataAccess
{
    public class MongoSugerenciaData : IMongoSugerenciaData
    {
        private readonly IDBConnection _db;
        private readonly IMemoryCache _cache;
        private readonly NotificadorSugerencias _notificador;
        private readonly IMongoCollection<SugerenciaModel> _sugerencias;
        private const string CacheName = "SugerenciaData";

        public MongoSugerenciaData(IDBConnection db, IMemoryCache cache, NotificadorSugerencias notificador)
        {
            this._db = db;
            this._cache = cache;
            this._notificador = notificador;
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
            _cache.Remove(sugerencia.Autor?.Id ?? "");
            _notificador.Notificar(sugerencia.Id, TipoCambio.Actualizada);
        }
        public async Task VotarSugerencia(string SugerenciaId, string UsuarioId)
        {
            var client = _db.Client;
            var db = client.GetDatabase(_db.DbName);
            var sugerenciaEnTransaccion = db.GetCollection<SugerenciaModel>(_db.SugerenciaNombreColeccion);
            var usuariosEnTransaccion = db.GetCollection<UsuarioModel>(_db.UsuarioNombreColeccion);

            using var session = await client.StartSessionAsync();
            // WithTransactionAsync reintenta cuando dos votos a la misma sugerencia chocan (WriteConflict).
            bool cambio = await session.WithTransactionAsync(async (s, ct) =>
            {
                var sugerencia = (await sugerenciaEnTransaccion.FindAsync(s, x => x.Id == SugerenciaId, cancellationToken: ct)).First(ct);

                bool? votado = Votos.Alternar(sugerencia, UsuarioId);
                if (votado is null)
                {
                    // No se puede votar la propia sugerencia
                    return false;
                }

                await sugerenciaEnTransaccion.ReplaceOneAsync(s, x => x.Id == SugerenciaId, sugerencia, cancellationToken: ct);

                var usuario = (await usuariosEnTransaccion.FindAsync(s, u => u.Id == UsuarioId, cancellationToken: ct)).First(ct);

                usuario.SugerenciasVotadas.RemoveAll(x => x.Id == SugerenciaId);
                if (votado == true)
                {
                    usuario.SugerenciasVotadas.Add(new SugerenciaBasicaModel(sugerencia));
                }

                await usuariosEnTransaccion.ReplaceOneAsync(s, u => u.Id == UsuarioId, usuario, cancellationToken: ct);
                return true;
            });

            if (cambio)
            {
                _cache.Remove(CacheName);
                _notificador.Notificar(SugerenciaId, TipoCambio.Voto);
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
                await sugerenciaEnTransaccion.InsertOneAsync(session, sugerencia);

                var usuariosEnTransaccion = db.GetCollection<UsuarioModel>(_db.UsuarioNombreColeccion);
                var usuario = (await usuariosEnTransaccion.FindAsync(session, u => u.Id == sugerencia.Autor.Id)).First();
                usuario.SugerenciasCreadas.Add(new SugerenciaBasicaModel(sugerencia));
                await usuariosEnTransaccion.ReplaceOneAsync(session, u => u.Id == usuario.Id, usuario);

                await session.CommitTransactionAsync();
                _cache.Remove(CacheName);
                _cache.Remove(sugerencia.Autor.Id);
                _notificador.Notificar(sugerencia.Id, TipoCambio.Creada);
            }
            catch
            {
                await session.AbortTransactionAsync();
                throw;
            }
        }

        public async Task<List<SugerenciaModel>> BuscarParecidas(string texto, int limite = 5)
        {
            string consulta = BusquedaParecidas.Consulta(texto);
            if (consulta is null)
            {
                return [];
            }

            var filtro = Builders<SugerenciaModel>.Filter.Text(consulta, new TextSearchOptions { Language = "spanish" })
                & Builders<SugerenciaModel>.Filter.Eq(s => s.AprobadoParaMostrar, true)
                & Builders<SugerenciaModel>.Filter.Eq(s => s.Archivado, false);

            return await _sugerencias.Find(filtro)
                .Sort(Builders<SugerenciaModel>.Sort.MetaTextScore("score"))
                .Limit(limite)
                .ToListAsync();
        }
    }
}
