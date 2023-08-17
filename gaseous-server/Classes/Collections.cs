using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using gaseous_tools;
using Newtonsoft.Json;

namespace gaseous_server.Classes
{
	public class Collections
	{
		public Collections()
		{
            
		}

        public class CollectionItem {
            public CollectionItem() {

            }

            public string Name { get; set; }
            public string Description { get; set; }
            public List<long> Platforms { get; set; }
            public List<long> Genres { get; set; }
            public List<long> Players { get; set; }
            public List<long> PlayerPerspectives { get; set; }
            public List<long> Themes { get; set; }
            public int MinimumRating { get; set; }
            public int MaximumRating { get; set; }
            public int MaximumRomsPerPlatform { get; set; }
            public long MaximumBytesPerPlatform { get; set; }
            public long MaximumCollectionSizeInBytes { get; set; }

            [JsonIgnore]
            public CollectionBuildStatus BuildStatus { get; set; }

            [JsonIgnore]
            public long CollectionBuiltSizeBytes { get; set; }

            [JsonIgnore]
            public long CollectionProjectedSizeBytes { get; set; }

            public enum CollectionBuildStatus {
                NoStatus = 0,
                NotBuilt = 1,
                Building = 2,
                Completed = 3
            }
        }
    }
}