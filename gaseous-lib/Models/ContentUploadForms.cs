using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using static gaseous_server.Classes.Content.ContentManager;

namespace gaseous_server.Models
{
    /// <summary>
    /// Form model for a single content file upload.
    /// </summary>
    public class SingleContentUploadForm
    {
        /// <summary>The uploaded file.</summary>
        [Required]
        public IFormFile File { get; set; } = null!;
        /// <summary>The logical content type classification.</summary>
        [Required]
        public ContentType ContentType { get; set; }
        /// <summary>Optional description.</summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Form model for multiple content file uploads.
    /// </summary>
    public class MultiContentUploadForm
    {
        /// <summary>The uploaded files.</summary>
        [Required]
        public List<IFormFile> Files { get; set; } = new();
        /// <summary>The logical content type classification applied to all files.</summary>
        [Required]
        public ContentType ContentType { get; set; }
        /// <summary>Optional description applied to each file.</summary>
        public string? Description { get; set; }
    }
}
