

using Microsoft.Extensions.Caching.Memory;

namespace SuegerenciasAppLibrary.DataAccess
{
    public class MongoCategoryData : ICategoryData
    {
        private readonly IMongoCollection<CategoriaModel> _categorias;
        private readonly IMemoryCache _cache;
        private const string CacheName = "CategoriaData";

        public MongoCategoryData(IDBConnection db, IMemoryCache cache)
        {
            _cache = cache;
            _categorias = db.CategoriaColeccion;
        }
        public async Task<List<CategoriaModel>> GetAllCategorias()
        {
            var output = _cache.Get<List<CategoriaModel>>(CacheName);
            if (output is null)
            {
                var resultado = await _categorias.FindAsync(_ => true);
                output = resultado.ToList();

                _cache.Set(CacheName, output, TimeSpan.FromDays(1));
            }
            return output;
        }

        public Task CreateCategoria(CategoriaModel categoria)
        {
            return _categorias.InsertOneAsync(categoria);
        }
    }
}
