using System.Net.Mime;
using gaseous_server.Classes.Content;

namespace gaseous_server.Models
{
    public class ContentModel
    {
        public gaseous_server.Classes.Content.ContentManager.ContentType ContentType { get; set; }
        public string ByteArrayBase64 { get; set; }
        public byte[] ByteArray
        {
            get
            {
                return Convert.FromBase64String(ByteArrayBase64);
            }
        }
        public string Filename { get; set; }
        public string? Description { get; set; }
    }

    public class ContentUpdateModel
    {
        public bool? IsShared { get; set; }
        public string? Content { get; set; }
    }

    public class ContentViewModel
    {
        public long AttachmentId { get; set; }
        public string FileName { get; set; }
        public ContentManager.ContentType ContentType { get; set; }
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; }
        public Models.UserProfile? UploadedBy { get; set; }
        // for internal use only
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string UploadedByUserId { get; set; }
        public bool IsShared { get; set; }
        public string FileExtension
        {
            get
            {
                switch (ContentType)
                {
                    case ContentManager.ContentType.GlobalManual:
                        return ".pdf";
                    case ContentManager.ContentType.Screenshot:
                    case ContentManager.ContentType.Photo:
                        return ".png";
                    case ContentManager.ContentType.Video:
                        return ".mp4";
                    case ContentManager.ContentType.AudioSample:
                        return ".zip";
                    case ContentManager.ContentType.Note:
                        return ".txt";
                    default:
                        return System.IO.Path.GetExtension(FileName);
                }
            }
        }
        public string FileMimeType
        {
            get
            {
                switch (ContentType)
                {
                    case ContentManager.ContentType.GlobalManual:
                        return MediaTypeNames.Application.Pdf;
                    case ContentManager.ContentType.Screenshot:
                    case ContentManager.ContentType.Photo:
                        return "image/png";
                    case ContentManager.ContentType.Video:
                        return "video/mp4";
                    case ContentManager.ContentType.AudioSample:
                        return "application/zip";
                    case ContentManager.ContentType.Note:
                        return "text/plain";
                    default:
                        return "application/octet-stream";
                }
            }
        }
    }
}