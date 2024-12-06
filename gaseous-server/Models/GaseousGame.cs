using System.Reflection;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
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
        public HasheousClient.Models.MetadataSources MetadataSource { get; set; }
    }

    internal class NoDatabaseAttribute : Attribute
    {
    }
}