using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Testcontainers.MongoDb;

namespace AppSugerencias.Tests.DataAccess;

/// <summary>
/// Levanta un MongoDB real en Docker, con replica set porque el acceso a datos usa transacciones.
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:8")
        .WithReplicaSet()
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>Crea una conexión a una base nueva, para que cada test arranque vacío.</summary>
    public IDBConnection NuevaConexion()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:MongoDB"] = _container.GetConnectionString(),
                ["DatabaseName"] = $"tests_{Guid.NewGuid():N}"
            })
            .Build();

        return new DBConnection(config);
    }

    public static IMemoryCache NuevoCache() => new MemoryCache(new MemoryCacheOptions());
}

[CollectionDefinition(Nombre)]
public sealed class MongoCollection : ICollectionFixture<MongoFixture>
{
    public const string Nombre = "Mongo";
}
