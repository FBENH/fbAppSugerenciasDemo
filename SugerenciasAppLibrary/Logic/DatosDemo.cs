namespace SugerenciasAppLibrary.Logic;

/// <summary>
/// Datos de ejemplo del demo. Se cargan cuando la base está vacía y, si está activado,
/// se vuelven a cargar periódicamente para deshacer lo que hagan los visitantes con el usuario admin público.
/// </summary>
public static class DatosDemo
{
    public const string PrefijoUsuarioDemo = "demo-";
    private const string ColeccionMeta = "meta";
    private const string IdReinicio = "reinicio-demo";

    private sealed record Ejemplo(
        string Titulo, string Descripcion, string Categoria, string Estado, int Autor, int[] Votantes,
        int DiasAtras, bool Aprobada = true, string Url = null, string[] Comentarios = null);

    private static readonly (string Nombre, string Descripcion)[] Categorias =
    [
        ("Backend", "APIs, arquitectura y lógica de servidor."),
        ("Frontend", "Interfaces web y frameworks de UI."),
        ("Bases de datos", "Modelado, consultas y rendimiento."),
        ("Cloud y DevOps", "Despliegue, CI/CD e infraestructura."),
    ];

    private static readonly (string Nombre, string Descripcion)[] Estados =
    [
        ("Completado", "Ya hay material publicado sobre el tema."),
        ("En revision", "El equipo está evaluando la sugerencia."),
        ("Proximamente", "El material está en preparación."),
        ("Rechazada", "No se va a hacer por ahora."),
    ];

    private static readonly string[] Usuarios = ["Lucía M.", "Martín R.", "Sofía P.", "Diego A.", "Valentina C.", "Joaquín F."];

    private static readonly Ejemplo[] Sugerencias =
    [
        new("Autenticación con JWT en ASP.NET Core",
            "Cómo emitir y validar tokens, refresh tokens y dónde guardarlos en el cliente.",
            "Backend", "Completado", 0, [1, 2, 3, 4], 40, Url: "https://learn.microsoft.com/aspnet/core/security/authentication/",
            Comentarios: ["Me sirvió mucho la parte de refresh tokens.", "¿Van a cubrir también OAuth con proveedores externos?"]),
        new("Índices en MongoDB y cómo leer explain()",
            "Cuándo conviene un índice compuesto y cómo saber si una consulta lo está usando.",
            "Bases de datos", "Proximamente", 1, [0, 2, 5], 21,
            Comentarios: ["Ideal si incluye ejemplos con agregaciones."]),
        new("Render modes de Blazor en .NET 10",
            "Diferencias entre static SSR, interactive server y WebAssembly, y cuándo usar cada uno.",
            "Frontend", "En revision", 2, [0, 1, 3, 4, 5], 12),
        new("Pipelines de GitHub Actions para .NET",
            "Build, tests y deploy a Azure con environments y aprobaciones manuales.",
            "Cloud y DevOps", null, 3, [0, 4], 9),
        new("Tests de integración con Testcontainers",
            "Levantar bases reales en Docker para los tests en lugar de usar mocks.",
            "Backend", null, 4, [1, 2], 6,
            Comentarios: ["Sí, por favor. Los mocks de la base nunca se comportan igual que la real."]),
        new("Transacciones en MongoDB",
            "Cuándo hacen falta, cómo se usan desde el driver de C# y qué cuesta en rendimiento.",
            "Bases de datos", null, 5, [3], 4),
        new("Accesibilidad en formularios web",
            "Etiquetas, mensajes de error y navegación con teclado.",
            "Frontend", "Rechazada", 0, [], 30),
        new("Observabilidad con OpenTelemetry",
            "Trazas, métricas y logs en una app ASP.NET Core, con un ejemplo en Azure Monitor.",
            "Cloud y DevOps", null, 1, [], 2),
        new("Caché distribuida con Redis",
            "Patrones de invalidación y qué pasa cuando hay varias instancias de la app.",
            "Backend", null, 2, [], 1, Aprobada: false),
        new("Migraciones de esquema sin downtime",
            "Cómo cambiar el modelo de datos en producción sin cortar el servicio.",
            "Bases de datos", null, 3, [], 0, Aprobada: false),
    ];

    /// <summary>Carga los datos de ejemplo solo si no hay categorías (base nueva). No borra nada.</summary>
    public static async Task<bool> SembrarSiVaciaAsync(IDBConnection db)
    {
        if (await db.CategoriaColeccion.CountDocumentsAsync(FilterDefinition<CategoriaModel>.Empty) > 0)
        {
            return false;
        }

        await SembrarAsync(db);
        return true;
    }

    /// <summary>
    /// Borra sugerencias, comentarios y usuarios de ejemplo y vuelve a cargar los datos del demo.
    /// Los usuarios reales se conservan, pero se les vacían las listas de sugerencias creadas y votadas.
    /// </summary>
    public static async Task ReiniciarAsync(IDBConnection db, DateTime ahora)
    {
        await db.SugerenciaColeccion.DeleteManyAsync(FilterDefinition<SugerenciaModel>.Empty);
        await db.ComentarioColeccion.DeleteManyAsync(FilterDefinition<ComentarioModel>.Empty);
        await db.CategoriaColeccion.DeleteManyAsync(FilterDefinition<CategoriaModel>.Empty);
        await db.EstadoColeccion.DeleteManyAsync(FilterDefinition<EstadoModel>.Empty);
        await db.UsuarioColeccion.DeleteManyAsync(
            Builders<UsuarioModel>.Filter.Regex(u => u.ObjectIdentifier, new BsonRegularExpression($"^{PrefijoUsuarioDemo}")));
        await db.UsuarioColeccion.UpdateManyAsync(
            FilterDefinition<UsuarioModel>.Empty,
            Builders<UsuarioModel>.Update
                .Set(u => u.SugerenciasCreadas, new List<SugerenciaBasicaModel>())
                .Set(u => u.SugerenciasVotadas, new List<SugerenciaBasicaModel>()));

        await SembrarAsync(db, ahora);
        await GuardarUltimoReinicioAsync(db, ahora);
    }

    public static async Task<DateTime?> UltimoReinicioAsync(IDBConnection db)
    {
        var doc = await db.Database.GetCollection<BsonDocument>(ColeccionMeta)
            .Find(Builders<BsonDocument>.Filter.Eq("_id", IdReinicio))
            .FirstOrDefaultAsync();
        return doc is null ? null : doc["Fecha"].ToUniversalTime();
    }

    public static Task GuardarUltimoReinicioAsync(IDBConnection db, DateTime fecha)
    {
        return db.Database.GetCollection<BsonDocument>(ColeccionMeta).ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", IdReinicio),
            new BsonDocument { { "_id", IdReinicio }, { "Fecha", fecha } },
            new ReplaceOptions { IsUpsert = true });
    }

    private static async Task SembrarAsync(IDBConnection db, DateTime? ahora = null)
    {
        DateTime hoy = ahora ?? DateTime.UtcNow;

        var categorias = Categorias.Select(c => new CategoriaModel { CategoriaNombre = c.Nombre, CategoriaDescripcion = c.Descripcion }).ToList();
        await db.CategoriaColeccion.InsertManyAsync(categorias);

        var estados = Estados.Select(e => new EstadoModel { EstadoNombre = e.Nombre, EstadoDescripcion = e.Descripcion }).ToList();
        await db.EstadoColeccion.InsertManyAsync(estados);

        var usuarios = Usuarios.Select((nombre, i) => new UsuarioModel
        {
            ObjectIdentifier = $"{PrefijoUsuarioDemo}{i}",
            DisplayName = nombre,
            Nombre = nombre.Split(' ')[0],
        }).ToList();
        await db.UsuarioColeccion.InsertManyAsync(usuarios);

        var sugerencias = new List<(SugerenciaModel Modelo, Ejemplo Ejemplo)>();
        foreach (var e in Sugerencias)
        {
            var estado = estados.FirstOrDefault(x => x.EstadoNombre == e.Estado);
            var modelo = new SugerenciaModel
            {
                Sugerencia = e.Titulo,
                Descripcion = e.Descripcion,
                Categoria = categorias.First(c => c.CategoriaNombre == e.Categoria),
                Autor = new UsuarioBasicoModel(usuarios[e.Autor]),
                FechaCreacion = hoy.AddDays(-e.DiasAtras).AddHours(-e.Votantes.Length),
                VotosDeUsuarios = e.Votantes.Select(v => usuarios[v].Id).ToHashSet(),
                AprobadoParaMostrar = e.Aprobada,
                SugerenciaEstado = estado,
                NotasAdmins = e.Estado is null ? null : NotaAdmin.TextoParaEstado(e.Estado),
                UrlRecurso = e.Url,
            };
            sugerencias.Add((modelo, e));
        }
        await db.SugerenciaColeccion.InsertManyAsync(sugerencias.Select(s => s.Modelo));

        foreach (var (modelo, ejemplo) in sugerencias)
        {
            usuarios[ejemplo.Autor].SugerenciasCreadas.Add(new SugerenciaBasicaModel(modelo));
            foreach (int v in ejemplo.Votantes)
            {
                usuarios[v].SugerenciasVotadas.Add(new SugerenciaBasicaModel(modelo));
            }
        }
        foreach (var usuario in usuarios)
        {
            await db.UsuarioColeccion.ReplaceOneAsync(u => u.Id == usuario.Id, usuario);
        }

        var comentarios = sugerencias
            .Where(s => s.Ejemplo.Comentarios is not null)
            .SelectMany(s => s.Ejemplo.Comentarios.Select((texto, i) => new ComentarioModel
            {
                SugerenciaId = s.Modelo.Id,
                Texto = texto,
                Autor = new UsuarioBasicoModel(usuarios[(s.Ejemplo.Autor + i + 1) % usuarios.Count]),
                Fecha = s.Modelo.FechaCreacion.AddDays(i + 1),
            }))
            .ToList();
        if (comentarios.Count > 0)
        {
            await db.ComentarioColeccion.InsertManyAsync(comentarios);
        }
    }
}
