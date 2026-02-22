ALTER TABLE `Games_Roms`
ADD COLUMN `SHA256` VARCHAR(64) NULL DEFAULT NULL AFTER `SHA1`,
ADD INDEX `SHA256` (`SHA256`),
ADD INDEX `SHA1` (`SHA1`),
ADD INDEX `CRC` (`CRC`);

ALTER TABLE `Signatures_Roms`
ADD COLUMN `SHA256` VARCHAR(64) NULL DEFAULT NULL AFTER `SHA1`,
ADD INDEX `SHA256` (`SHA256`),
ADD INDEX `CRC` (`CRC`);

CREATE OR REPLACE VIEW `view_Games_Roms` AS
SELECT
    `Games_Roms`.`Id` AS `Id`,
    `Games_Roms`.`PlatformId` AS `PlatformId`,
    `view_MetadataMap`.`Id` AS `MetadataMapId`,
    `view_MetadataMap`.`MetadataSourceType` AS `GameIdType`,
    `view_MetadataMap`.`MetadataSourceId` AS `GameId`,
    `view_MetadataMap`.`UserManualLink` AS `UserManualLink`,
    `Games_Roms`.`Name` AS `Name`,
    `Games_Roms`.`Size` AS `Size`,
    `Games_Roms`.`CRC` AS `CRC`,
    `Games_Roms`.`MD5` AS `MD5`,
    `Games_Roms`.`SHA1` AS `SHA1`,
    `Games_Roms`.`SHA256` AS `SHA256`,
    `Games_Roms`.`DevelopmentStatus` AS `DevelopmentStatus`,
    `Games_Roms`.`Flags` AS `Flags`,
    `Games_Roms`.`Attributes` AS `Attributes`,
    `Games_Roms`.`RomType` AS `RomType`,
    `Games_Roms`.`RomTypeMedia` AS `RomTypeMedia`,
    `Games_Roms`.`MediaLabel` AS `MediaLabel`,
    `Games_Roms`.`RelativePath` AS `RelativePath`,
    `Games_Roms`.`MetadataSource` AS `MetadataSource`,
    `Games_Roms`.`MetadataGameName` AS `MetadataGameName`,
    `Games_Roms`.`MetadataVersion` AS `MetadataVersion`,
    `Games_Roms`.`LibraryId` AS `LibraryId`,
    `Games_Roms`.`LastMatchAttemptDate` AS `LastMatchAttemptDate`,
    `Games_Roms`.`DateCreated` AS `DateCreated`,
    `Games_Roms`.`DateUpdated` AS `DateUpdated`,
    `Games_Roms`.`RomDataVersion` AS `RomDataVersion`,
    CONCAT(
        `GameLibraries`.`Path`,
        '/',
        `Games_Roms`.`RelativePath`
    ) AS `Path`,
    `GameLibraries`.`Name` AS `LibraryName`
FROM (
        (
            `Games_Roms`
            JOIN `GameLibraries` ON (
                `Games_Roms`.`LibraryId` = `GameLibraries`.`Id`
            )
        )
        LEFT JOIN `view_MetadataMap` ON (
            `Games_Roms`.`MetadataMapId` = `view_MetadataMap`.`Id`
        )
    );

CREATE OR REPLACE VIEW `view_GamesWithRoms` AS
SELECT DISTINCT
    `Games_Roms`.`GameId` AS `ROMGameId`,
    `view_MetadataMap`.`Id` AS `MetadataMapId`,
    `view_MetadataMap`.`MetadataSourceType` AS `GameIdType`,
    CASE
        WHEN `Game`.`Id` IS NULL THEN 0
        ELSE `Game`.`Id`
    END AS `Id`,
    `Game`.`AgeRatings` AS `AgeRatings`,
    `Game`.`AggregatedRating` AS `AggregatedRating`,
    `Game`.`AggregatedRatingCount` AS `AggregatedRatingCount`,
    `Game`.`AlternativeNames` AS `AlternativeNames`,
    `Game`.`Artworks` AS `Artworks`,
    `Game`.`Bundles` AS `Bundles`,
    `Game`.`Category` AS `Category`,
    `Game`.`Checksum` AS `Checksum`,
    `Game`.`Collection` AS `Collection`,
    `Game`.`Cover` AS `Cover`,
    `Game`.`CreatedAt` AS `CreatedAt`,
    `Game`.`Dlcs` AS `Dlcs`,
    `Game`.`Expansions` AS `Expansions`,
    `Game`.`ExternalGames` AS `ExternalGames`,
    `Game`.`FirstReleaseDate` AS `FirstReleaseDate`,
    `Game`.`Follows` AS `Follows`,
    `Game`.`Franchise` AS `Franchise`,
    `Game`.`Franchises` AS `Franchises`,
    `Game`.`GameEngines` AS `GameEngines`,
    `Game`.`GameModes` AS `GameModes`,
    `Game`.`Genres` AS `Genres`,
    `Game`.`Hypes` AS `Hypes`,
    `Game`.`InvolvedCompanies` AS `InvolvedCompanies`,
    `Game`.`Keywords` AS `Keywords`,
    `Game`.`MultiplayerModes` AS `MultiplayerModes`,
    CASE
        WHEN `Game`.`Name` IS NULL THEN `view_MetadataMap`.`SignatureGameName`
        ELSE `Game`.`Name`
    END AS `Name`,
    CASE
        WHEN `Game`.`Name` IS NULL THEN CASE
            WHEN `view_MetadataMap`.`SignatureGameName` LIKE 'The %' THEN CONCAT(
                TRIM(
                    SUBSTR(
                        `view_MetadataMap`.`SignatureGameName`,
                        4
                    )
                ),
                ', The'
            )
            ELSE `view_MetadataMap`.`SignatureGameName`
        END
        WHEN `Game`.`Name` LIKE 'The %' THEN CONCAT(
            TRIM(SUBSTR(`Game`.`Name`, 4)),
            ', The'
        )
        ELSE `Game`.`Name`
    END AS `NameThe`,
    `Game`.`ParentGame` AS `ParentGame`,
    `Game`.`Platforms` AS `Platforms`,
    `Game`.`PlayerPerspectives` AS `PlayerPerspectives`,
    `Game`.`Rating` AS `Rating`,
    `Game`.`RatingCount` AS `RatingCount`,
    `Game`.`ReleaseDates` AS `ReleaseDates`,
    `Game`.`Screenshots` AS `Screenshots`,
    `Game`.`SimilarGames` AS `SimilarGames`,
    `Game`.`Slug` AS `Slug`,
    `Game`.`StandaloneExpansions` AS `StandaloneExpansions`,
    `Game`.`Status` AS `Status`,
    `Game`.`StoryLine` AS `StoryLine`,
    `Game`.`Summary` AS `Summary`,
    `Game`.`Tags` AS `Tags`,
    `Game`.`Themes` AS `Themes`,
    `Game`.`TotalRating` AS `TotalRating`,
    `Game`.`TotalRatingCount` AS `TotalRatingCount`,
    `Game`.`UpdatedAt` AS `UpdatedAt`,
    `Game`.`Url` AS `Url`,
    `Game`.`VersionParent` AS `VersionParent`,
    `Game`.`VersionTitle` AS `VersionTitle`,
    `Game`.`Videos` AS `Videos`,
    `Game`.`Websites` AS `Websites`,
    `Game`.`dateAdded` AS `dateAdded`,
    `Game`.`lastUpdated` AS `lastUpdated`,
    COUNT(`Games_Roms`.`Id`) AS `RomCount`
FROM (
        (
            `Games_Roms`
            JOIN `view_MetadataMap` ON (
                `view_MetadataMap`.`Id` = `Games_Roms`.`MetadataMapId`
            )
        )
        LEFT JOIN `Metadata_Game` AS `Game` ON (
            `Game`.`SourceId` = `view_MetadataMap`.`MetadataSourceType`
            AND `Game`.`Id` = `view_MetadataMap`.`MetadataSourceId`
        )
    )
GROUP BY
    `view_MetadataMap`.`Id`;

CREATE OR REPLACE VIEW `view_Games` AS
SELECT
    `a`.`ROMGameId` AS `ROMGameId`,
    `a`.`Id` AS `Id`,
    `a`.`GameIdType` AS `GameIdType`,
    `a`.`AgeRatings` AS `AgeRatings`,
    `a`.`AggregatedRating` AS `AggregatedRating`,
    `a`.`AggregatedRatingCount` AS `AggregatedRatingCount`,
    `a`.`AlternativeNames` AS `AlternativeNames`,
    `a`.`Artworks` AS `Artworks`,
    `a`.`Bundles` AS `Bundles`,
    `a`.`Category` AS `Category`,
    `a`.`Checksum` AS `Checksum`,
    `a`.`Collection` AS `Collection`,
    `a`.`Cover` AS `Cover`,
    `a`.`CreatedAt` AS `CreatedAt`,
    `a`.`Dlcs` AS `Dlcs`,
    `a`.`Expansions` AS `Expansions`,
    `a`.`ExternalGames` AS `ExternalGames`,
    `a`.`FirstReleaseDate` AS `FirstReleaseDate`,
    `a`.`Follows` AS `Follows`,
    `a`.`Franchise` AS `Franchise`,
    `a`.`Franchises` AS `Franchises`,
    `a`.`GameEngines` AS `GameEngines`,
    `a`.`GameModes` AS `GameModes`,
    `a`.`Genres` AS `Genres`,
    `a`.`Hypes` AS `Hypes`,
    `a`.`InvolvedCompanies` AS `InvolvedCompanies`,
    `a`.`Keywords` AS `Keywords`,
    `a`.`MultiplayerModes` AS `MultiplayerModes`,
    `a`.`Name` AS `Name`,
    `a`.`ParentGame` AS `ParentGame`,
    `a`.`Platforms` AS `Platforms`,
    `a`.`PlayerPerspectives` AS `PlayerPerspectives`,
    `a`.`Rating` AS `Rating`,
    `a`.`RatingCount` AS `RatingCount`,
    `a`.`ReleaseDates` AS `ReleaseDates`,
    `a`.`Screenshots` AS `Screenshots`,
    `a`.`SimilarGames` AS `SimilarGames`,
    `a`.`Slug` AS `Slug`,
    `a`.`StandaloneExpansions` AS `StandaloneExpansions`,
    `a`.`Status` AS `Status`,
    `a`.`StoryLine` AS `StoryLine`,
    `a`.`Summary` AS `Summary`,
    `a`.`Tags` AS `Tags`,
    `a`.`Themes` AS `Themes`,
    `a`.`TotalRating` AS `TotalRating`,
    `a`.`TotalRatingCount` AS `TotalRatingCount`,
    `a`.`UpdatedAt` AS `UpdatedAt`,
    `a`.`Url` AS `Url`,
    `a`.`VersionParent` AS `VersionParent`,
    `a`.`VersionTitle` AS `VersionTitle`,
    `a`.`Videos` AS `Videos`,
    `a`.`Websites` AS `Websites`,
    `a`.`dateAdded` AS `dateAdded`,
    `a`.`lastUpdated` AS `lastUpdated`,
    `a`.`NameThe` AS `NameThe`,
    `b`.`AgeGroupId` AS `AgeGroupId`
FROM (
        `view_GamesWithRoms` `a`
        LEFT JOIN `Metadata_AgeGroup` `b` ON (`b`.`GameId` = `a`.`Id`)
    )
ORDER BY `a`.`NameThe`;

-- CREATE INDEX `idx_Relation_Game_Genres_GameId_GameSourceId` ON `Relation_Game_Genres` (`GameId`, `GameSourceId`);

-- CREATE INDEX `idx_Relation_Game_GameModes_GameId_GameSourceId` ON `Relation_Game_GameModes` (`GameId`, `GameSourceId`);

-- CREATE INDEX `idx_Relation_Game_PlayerPerspectives_GameId_GameSourceId` ON `Relation_Game_PlayerPerspectives` (`GameId`, `GameSourceId`);

-- CREATE INDEX `idx_Relation_Game_Themes_GameId_GameSourceId` ON `Relation_Game_Themes` (`GameId`, `GameSourceId`);