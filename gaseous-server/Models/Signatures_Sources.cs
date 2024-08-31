using NuGet.Protocol.Core.Types;

namespace gaseous_server.Models
{
    public class Signatures_Sources
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string URL { get; set; }
        public string Category { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Email { get; set; }
        public string Homepage { get; set; }
        public gaseous_signature_parser.parser.SignatureParser SourceType { get; set; }
        public string MD5 { get; set; }
        public string SHA1 { get; set; }
    }
}