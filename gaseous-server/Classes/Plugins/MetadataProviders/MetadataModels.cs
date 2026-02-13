using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes
{
    /// <summary>
    /// Extended metadata models that wrap metadata types with source tracking.
    /// Each model inherits from the corresponding base class and adds a SourceType property
    /// to track which metadata source provided the data.
    /// </summary>

    /// <summary>
    /// Represents an age rating classification with source tracking.
    /// Extends the AgeRating model to include metadata source information.
    /// </summary>
    public class AgeRating : HasheousClient.Models.Metadata.IGDB.AgeRating
    {
        /// <summary>
        /// Gets or sets the source that provided this age rating metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents an age rating category with source tracking.
    /// Extends the AgeRatingCategory model to include metadata source information.
    /// </summary>
    public class AgeRatingCategory : HasheousClient.Models.Metadata.IGDB.AgeRatingCategory
    {
        /// <summary>
        /// Gets or sets the source that provided this age rating category metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents an age rating content description with source tracking.
    /// Extends the AgeRatingContentDescriptionV2 model to include metadata source information.
    /// </summary>
    public class AgeRatingContentDescription : HasheousClient.Models.Metadata.IGDB.AgeRatingContentDescriptionV2
    {
        /// <summary>
        /// Gets or sets the source that provided this age rating content description metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents an age rating organization with source tracking.
    /// Extends the AgeRatingOrganization model to include metadata source information.
    /// </summary>
    public class AgeRatingOrganization : HasheousClient.Models.Metadata.IGDB.AgeRatingOrganization
    {
        /// <summary>
        /// Gets or sets the source that provided this age rating organization metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents an alternative game name with source tracking.
    /// Extends the AlternativeName model to include metadata source information.
    /// </summary>
    public class AlternativeName : HasheousClient.Models.Metadata.IGDB.AlternativeName
    {
        /// <summary>
        /// Gets or sets the source that provided this alternative name metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents game artwork with source tracking.
    /// Extends the Artwork model to include metadata source information.
    /// </summary>
    public class Artwork : HasheousClient.Models.Metadata.IGDB.Artwork
    {
        /// <summary>
        /// Gets or sets the source that provided this artwork metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }

        /// <summary>
        /// Gets or sets the name of the metadata provider that supplied this artwork metadata. This property is not mapped to the database and is used for in-memory tracking of the metadata provider associated with this artwork.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public string? ProviderName { get; set; }

        /// <summary>
        /// Gets the image paths for this artwork based on its metadata source and ID.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public gaseous_server.Classes.Metadata.ImagePath Paths
        {
            get
            {
                return new gaseous_server.Classes.Metadata.ImagePath(SourceType, ProviderName, Game, gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.ImageType.Artwork, ImageId);
            }
        }
    }

    /// <summary>
    /// Represents a clear logo image with source tracking.
    /// Extends the ClearLogo model to include metadata source information.
    /// </summary>
    public class ClearLogo : HasheousClient.Models.Metadata.IGDB.ClearLogo
    {
        /// <summary>
        /// Gets or sets the source that provided this clear logo metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }

        /// <summary>
        /// Gets or sets the name of the metadata provider that supplied this clear logo metadata. This property is not mapped to the database and is used for in-memory tracking of the metadata provider associated with this clear logo.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public string? ProviderName { get; set; }

        /// <summary>
        /// Gets the image paths for this clear logo based on its metadata source and ID.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public gaseous_server.Classes.Metadata.ImagePath Paths
        {
            get
            {
                return new gaseous_server.Classes.Metadata.ImagePath(SourceType, ProviderName, Game, gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.ImageType.ClearLogo, ImageId);
            }
        }
    }

    /// <summary>
    /// Represents a game collection with source tracking.
    /// Extends the Collection model to include metadata source information.
    /// </summary>
    public class Collection : HasheousClient.Models.Metadata.IGDB.Collection
    {
        /// <summary>
        /// Gets or sets the source that provided this collection metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a game company with source tracking.
    /// Extends the Company model to include metadata source information.
    /// </summary>
    public class Company : HasheousClient.Models.Metadata.IGDB.Company
    {
        /// <summary>
        /// Gets or sets the source that provided this company metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a company logo with source tracking.
    /// Extends the CompanyLogo model to include metadata source information.
    /// </summary>
    public class CompanyLogo : HasheousClient.Models.Metadata.IGDB.CompanyLogo
    {
        /// <summary>
        /// Gets or sets the source that provided this company logo metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a game cover image with source tracking.
    /// Extends the Cover model to include metadata source information.
    /// </summary>
    public class Cover : HasheousClient.Models.Metadata.IGDB.Cover
    {
        /// <summary>
        /// Gets or sets the source that provided this cover image metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }

        /// <summary>
        /// Gets or sets the name of the metadata provider that supplied this cover image metadata. This property is not mapped to the database and is used for in-memory tracking of the metadata provider associated with this cover image.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public string? ProviderName { get; set; }

        /// <summary>
        /// Gets the image paths for this cover based on its metadata source and ID.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public gaseous_server.Classes.Metadata.ImagePath Paths
        {
            get
            {
                return new gaseous_server.Classes.Metadata.ImagePath(SourceType, ProviderName, Game, gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.ImageType.Cover, ImageId);
            }
        }
    }

    /// <summary>
    /// Represents an external game reference with source tracking.
    /// Extends the ExternalGame model to include metadata source information.
    /// </summary>
    public class ExternalGame : HasheousClient.Models.Metadata.IGDB.ExternalGame
    {
        /// <summary>
        /// Gets or sets the source that provided this external game metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a game franchise with source tracking.
    /// Extends the Franchise model to include metadata source information.
    /// </summary>
    public class Franchise : HasheousClient.Models.Metadata.IGDB.Franchise
    {
        /// <summary>
        /// Gets or sets the source that provided this franchise metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents game localization information with source tracking.
    /// Extends the GameLocalization model to include metadata source information.
    /// </summary>
    public class GameLocalization : HasheousClient.Models.Metadata.IGDB.GameLocalization
    {
        /// <summary>
        /// Gets or sets the source that provided this game localization metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a game mode with source tracking.
    /// Extends the GameMode model to include metadata source information.
    /// </summary>
    public class GameMode : HasheousClient.Models.Metadata.IGDB.GameMode
    {
        /// <summary>
        /// Gets or sets the source that provided this game mode metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a game with source tracking.
    /// Extends the Game model to include metadata source information.
    /// </summary>
    public class Game : HasheousClient.Models.Metadata.IGDB.Game
    {
        /// <summary>
        /// Gets or sets the source that provided this game metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }

        /// <summary>
        /// Gets the metadata source for this game, which is determined by the SourceType property.
        /// This value is deprecated and exists as a legacy property for backward compatibility. Use the SourceType property directly to determine the metadata source.
        /// </summary>
        public FileSignature.MetadataSources MetadataSource
        {
            get
            {
                return SourceType;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this game is marked as a favorite by the user. This property is not mapped to the database and is used for in-memory tracking of user preferences.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public bool IsFavourite { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this game has a saved game associated with it. This property is not mapped to the database and is used for in-memory tracking of saved game availability.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public bool HasSavedGame { get; set; } = false;

        /// <summary>
        /// Gets or sets the ID of the metadata map associated with this game. This property is not mapped to the database and is used for in-memory tracking of metadata mapping.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public long MetadataMapId { get; set; }

        /// <summary>
        /// Gets or sets a dictionary mapping metadata sources to lists of IDs for clear logo images associated with this game. This property is not mapped to the database and is used for in-memory tracking of clear logo metadata from different sources.
        /// The dictionary keys represent the metadata sources, and the values are lists of clear logo IDs provided by each source.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public Dictionary<FileSignature.MetadataSources, List<long>> ClearLogos { get; set; }

        /// <summary>
        /// Gets or sets a list of IDs for clear logo images associated with this game.
        /// </summary>
        public List<long> ClearLogo { get; set; } = new List<long>();


    }

    /// <summary>
    /// Represents a game video with source tracking.
    /// Extends the GameVideo model to include metadata source information.
    /// </summary>
    public class GameVideo : HasheousClient.Models.Metadata.IGDB.GameVideo
    {
        /// <summary>
        /// Gets or sets the source that provided this game video metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a game genre with source tracking.
    /// Extends the Genre model to include metadata source information.
    /// </summary>
    public class Genre : HasheousClient.Models.Metadata.IGDB.Genre
    {
        /// <summary>
        /// Gets or sets the source that provided this genre metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a company involved in game development with source tracking.
    /// Extends the InvolvedCompany model to include metadata source information.
    /// </summary>
    public class InvolvedCompany : HasheousClient.Models.Metadata.IGDB.InvolvedCompany
    {
        /// <summary>
        /// Gets or sets the source that provided this involved company metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a multiplayer mode with source tracking.
    /// Extends the MultiplayerMode model to include metadata source information.
    /// </summary>
    public class MultiplayerMode : HasheousClient.Models.Metadata.IGDB.MultiplayerMode
    {
        /// <summary>
        /// Gets or sets the source that provided this multiplayer mode metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a platform logo with source tracking.
    /// Extends the PlatformLogo model to include metadata source information.
    /// </summary>
    public class PlatformLogo : HasheousClient.Models.Metadata.IGDB.PlatformLogo
    {
        /// <summary>
        /// Gets or sets the source that provided this platform logo metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a gaming platform with source tracking.
    /// Extends the Platform model to include metadata source information.
    /// </summary>
    public class Platform : HasheousClient.Models.Metadata.IGDB.Platform
    {
        /// <summary>
        /// Gets or sets the source that provided this platform metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a gaming platform version with source tracking.
    /// Extends the PlatformVersion model to include metadata source information.
    /// </summary>
    public class PlatformVersion : HasheousClient.Models.Metadata.IGDB.PlatformVersion
    {
        /// <summary>
        /// Gets or sets the source that provided this platform version metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a player perspective type with source tracking.
    /// Extends the PlayerPerspective model to include metadata source information.
    /// </summary>
    public class PlayerPerspective : HasheousClient.Models.Metadata.IGDB.PlayerPerspective
    {
        /// <summary>
        /// Gets or sets the source that provided this player perspective metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a geographic region with source tracking.
    /// Extends the Region model to include metadata source information.
    /// </summary>
    public class Region : HasheousClient.Models.Metadata.IGDB.Region
    {
        /// <summary>
        /// Gets or sets the source that provided this region metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a game release date with source tracking.
    /// Extends the ReleaseDate model to include metadata source information.
    /// </summary>
    public class ReleaseDate : HasheousClient.Models.Metadata.IGDB.ReleaseDate
    {
        /// <summary>
        /// Gets or sets the source that provided this release date metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Represents a game screenshot with source tracking.
    /// Extends the Screenshot model to include metadata source information.
    /// </summary>
    public class Screenshot : HasheousClient.Models.Metadata.IGDB.Screenshot
    {
        /// <summary>
        /// Gets or sets the source that provided this screenshot metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }

        /// <summary>
        /// Gets or sets the name of the metadata provider that supplied this screenshot metadata. This property is not mapped to the database and is used for in-memory tracking of the metadata provider associated with this screenshot.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public string? ProviderName { get; set; }

        /// <summary>
        /// Gets the image paths for this screenshot based on its metadata source and ID.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public gaseous_server.Classes.Metadata.ImagePath Paths
        {
            get
            {
                return new gaseous_server.Classes.Metadata.ImagePath(SourceType, ProviderName, Game, gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.ImageType.Screenshot, ImageId);
            }
        }
    }

    /// <summary>
    /// Represents a game theme with source tracking.
    /// Extends the Theme model to include metadata source information.
    /// </summary>
    public class Theme : HasheousClient.Models.Metadata.IGDB.Theme
    {
        /// <summary>
        /// Gets or sets the source that provided this theme metadata.
        /// </summary>
        [Models.NoDatabaseAttribute]
        public FileSignature.MetadataSources SourceType { get; set; }
    }

    /// <summary>
    /// Specifies the type of search query to use when retrieving metadata.
    /// </summary>
    public enum SearchType
    {
        /// <summary>
        /// Standard where clause search - exact match.
        /// </summary>
        where = 0,

        /// <summary>
        /// Standard where clause search - wildcard match.
        /// </summary>
        wherefuzzy = 1,

        /// <summary>
        /// Full-text search query.
        /// </summary>
        search = 2,

        /// <summary>
        /// Search query without platform filtering.
        /// </summary>
        searchNoPlatform = 3
    }

    /// <summary>
    /// Specifies the type of image asset for metadata.
    /// </summary>
    public enum ImageType
    {
        /// <summary>
        /// Represents a cover image asset.
        /// </summary>
        Cover,

        /// <summary>
        /// Represents a screenshot image asset.
        /// </summary>
        Screenshot,

        /// <summary>
        /// Represents an artwork image asset.
        /// </summary>
        Artwork,

        /// <summary>
        /// Represents a clear logo image asset.
        /// </summary>
        ClearLogo,

        /// <summary>
        /// Represents a platform logo image asset.
        /// </summary>
        PlatformLogo
    }
}