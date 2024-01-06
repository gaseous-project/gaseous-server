using System.Reflection;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace gaseous_server.Models
{
    public class GaseousGame : IGDB.Models.Game
    {
        public GaseousGame()
        {

        }

        public GaseousGame(IGDB.Models.Game game)
        {
            var targetType = this.GetType();
            var sourceType = game.GetType();
            foreach(var prop in targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public| BindingFlags.SetProperty))
            {
                // check whether source object has the the property
                var sp = sourceType.GetProperty(prop.Name);
                if (sp != null)
                {
                    // if yes, copy the value to the matching property
                    var value = sp.GetValue(game, null);
                    prop.SetValue(this, value, null);
                }
            }
        }

        public IGDB.Models.Cover? CoverItem
        {
            get
            {
                if (this.Cover != null)
                {
                    if (this.Cover.Id != null)
                    {
                        IGDB.Models.Cover cover = Covers.GetCover(Cover.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(this), false);

                        return cover;
                    }
                }

                return null;
            }
        }

        public List<IGDB.Models.Artwork>? ArtworksItem
        {
            get
            {
                if (this.Artworks != null)
                {
                    if (this.Artworks.Ids != null)
                    {
                        List<IGDB.Models.Artwork> artworks = new List<IGDB.Models.Artwork>();
                        foreach (long id in this.Artworks.Ids)
                        {
                            artworks.Add(gaseous_server.Classes.Metadata.Artworks.GetArtwork(id, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(this), false));
                        }

                        return artworks;
                    }
                }

                return null;
            }
        }

        public List<IGDB.Models.Screenshot>? ScreenshotsItem
        {
            get
            {
                if (this.Screenshots != null)
                {
                    if (this.Screenshots.Ids != null)
                    {
                        List<IGDB.Models.Screenshot> screenshots = new List<IGDB.Models.Screenshot>();
                        foreach (long id in this.Screenshots.Ids)
                        {
                            screenshots.Add(gaseous_server.Classes.Metadata.Screenshots.GetScreenshot(id, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(this), false));
                        }

                        return screenshots;
                    }
                }

                return null;
            }
        }
    }
}