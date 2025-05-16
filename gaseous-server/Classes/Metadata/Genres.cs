using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class Genres
    {
        public const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        static List<GenreItem> genreItemCache = new List<GenreItem>();

        public Genres()
        {
        }

        public static async Task<Genre?> GetGenres(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                // check cache for genre
                if (genreItemCache.Find(x => x.Id == Id && x.SourceType == SourceType) != null)
                {
                    GenreItem genreItem = genreItemCache.Find(x => x.Id == Id && x.SourceType == SourceType);

                    Genre? nGenre = new Genre
                    {
                        Id = genreItem.Id,
                        Name = genreItem.Name
                    };

                    return nGenre;
                }

                // get genre from metadata
                Genre? RetVal = await Metadata.GetMetadataAsync<Genre>(SourceType, (long)Id, false);

                if (RetVal != null)
                {
                    // add genre to cache
                    if (genreItemCache.Find(x => x.Id == Id && x.SourceType == SourceType) == null)
                    {
                        GenreItem genreItem = new GenreItem();
                        genreItem.Id = (long)Id;
                        genreItem.SourceType = SourceType;
                        genreItem.Name = RetVal.Name;
                        genreItemCache.Add(genreItem);
                    }
                }

                return RetVal;
            }
        }
    }

    class GenreItem
    {
        public long Id { get; set; }
        public HasheousClient.Models.MetadataSources SourceType { get; set; }
        public string Name { get; set; }
    }
}

