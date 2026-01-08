using System.Reflection;
using System.Text.Json.Serialization;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace gaseous_server.Models
{
    public class Game : HasheousClient.Models.Metadata.IGDB.Game
    {
        [NoDatabase]
        public bool IsFavourite { get; set; } = false;

        [NoDatabase]
        public bool HasSavedGame { get; set; } = false;

        [NoDatabase]
        public long MetadataMapId { get; set; }

        [NoDatabase]
        public FileSignature.MetadataSources MetadataSource { get; set; }

        [NoDatabase]
        public Dictionary<FileSignature.MetadataSources, List<long>> ClearLogo { get; set; }
    }

    /// <summary>
    /// Indicates that a property should not be mapped to the database.
    /// </summary>
    internal class NoDatabaseAttribute : Attribute
    {
    }
}