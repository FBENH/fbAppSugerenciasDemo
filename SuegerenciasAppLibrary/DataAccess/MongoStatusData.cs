

using Microsoft.Extensions.Caching.Memory;
using System.Data;

namespace SuegerenciasAppLibrary.DataAccess
{
    public class MongoStatusData : IStatusData
    {
        private readonly IMongoCollection<EstadoModel> _estados;
        private readonly IMemoryCache _cache;
        private const string CacheName = "StatusData";

        public MongoStatusData(IDBConnection db, IMemoryCache cache)
        {
            _cache = cache;
            _estados = db.EstadoColeccion;
        }

        public async Task<List<EstadoModel>> GetAllEstados()
        {
            var output = _cache.Get<List<EstadoModel>>(CacheName);
            if (output is null)
            {
                var resultado = await _estados.FindAsync(_ => true);
                output = resultado.ToList();

                _cache.Set(CacheName, output, TimeSpan.FromDays(1));
            }

            return output;
        }

        public Task CreateEstados(EstadoModel estado)
        {
            return _estados.InsertOneAsync(estado);
        }
    }
}
