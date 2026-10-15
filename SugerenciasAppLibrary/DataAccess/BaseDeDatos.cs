namespace SugerenciasAppLibrary.DataAccess;

public static class BaseDeDatos
{
    /// <summary>Crea los índices que usa la app. Es idempotente: si ya existen, no hace nada.</summary>
    public static async Task CrearIndicesAsync(IDBConnection db)
    {
        var texto = Builders<SugerenciaModel>.IndexKeys
            .Text(s => s.Sugerencia)
            .Text(s => s.Descripcion);
        await db.SugerenciaColeccion.Indexes.CreateOneAsync(new CreateIndexModel<SugerenciaModel>(texto,
            new CreateIndexOptions
            {
                Name = "busqueda_texto",
                DefaultLanguage = "spanish",
                Weights = new BsonDocument { { nameof(SugerenciaModel.Sugerencia), 3 }, { nameof(SugerenciaModel.Descripcion), 1 } }
            }));

        await db.ComentarioColeccion.Indexes.CreateOneAsync(new CreateIndexModel<ComentarioModel>(
            Builders<ComentarioModel>.IndexKeys.Ascending(c => c.SugerenciaId).Ascending(c => c.Fecha),
            new CreateIndexOptions { Name = "comentarios_por_sugerencia" }));
    }
}
