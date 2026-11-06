CREATE TABLE IF NOT EXISTS `MetadataMapAgeGroup` (
    `MetadataMapId` bigint(20) NOT NULL,
    `AgeGroupId` int(11) NULL,
    `dateAdded` datetime DEFAULT CURRENT_TIMESTAMP,
    `lastUpdated` datetime DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`MetadataMapId`),
    INDEX `idx_MetadataMapAgeGroup_AgeGroupId` (`AgeGroupId`),
    CONSTRAINT `MetadataMapAgeGroup_MetadataMap` FOREIGN KEY (`MetadataMapId`) REFERENCES `MetadataMap` (`Id`) ON DELETE CASCADE
);

-- backfill: highest age group across every source of a map (MAX ignores unrated sources)
INSERT IGNORE INTO
    `MetadataMapAgeGroup` (`MetadataMapId`, `AgeGroupId`)
SELECT `b`.`ParentMapId`, MAX(`ag`.`AgeGroupId`)
FROM
    `MetadataMapBridge` `b`
    INNER JOIN (
        SELECT
            `GameId`,
            `SourceId`,
            `AgeGroupId`
        FROM `Metadata_AgeGroup`
        UNION ALL
        SELECT
            `Id` AS `GameId`,
            `SourceId`,
            `AgeGroupId`
        FROM `Metadata_Game`
    ) `ag` ON `ag`.`GameId` = `b`.`MetadataSourceId`
    AND `ag`.`SourceId` = `b`.`MetadataSourceType`
GROUP BY
    `b`.`ParentMapId`;

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
        LEFT JOIN `MetadataMapAgeGroup` `b` ON (
            `b`.`MetadataMapId` = `a`.`MetadataMapId`
        )
    )
ORDER BY `a`.`NameThe`;