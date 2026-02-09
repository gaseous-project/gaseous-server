using System.Reflection;
using System.Text.Json.Serialization;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace gaseous_server.Models
{
    /// <summary>
    /// Indicates that a property should not be mapped to the database.
    /// </summary>
    internal class NoDatabaseAttribute : Attribute
    {
    }
}