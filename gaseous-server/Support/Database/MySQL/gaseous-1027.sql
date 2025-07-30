ALTER TABLE `Signatures_Games`
CHANGE `Year` `Year` varchar(50) DEFAULT NULL;

ALTER TABLE `Signatures_Roms`
CHANGE `MediaLabel` `MediaLabel` varchar(255) DEFAULT NULL;

ALTER TABLE `Games_Roms` ADD COLUMN `RomDataVersion` INT DEFAULT 1;

CREATE TABLE UserProfiles (
    `Id` VARCHAR(45) NOT NULL,
    `UserId` VARCHAR(45) NOT NULL,
    `DisplayName` VARCHAR(255) NOT NULL,
    `Quip` VARCHAR(255) NOT NULL,
    `Avatar` LONGBLOB,
    `AvatarExtension` CHAR(6),
    `AvatarHash` VARCHAR(128),
    `ProfileBackground` LONGBLOB,
    `ProfileBackgroundExtension` CHAR(6),
    `ProfileBackgroundHash` VARCHAR(128),
    `UnstructuredData` LONGTEXT NOT NULL,
    PRIMARY KEY (`Id`, `UserId`)
);

ALTER TABLE `PlatformMap_Bios`
ADD COLUMN `Enabled` BOOLEAN DEFAULT TRUE;

CREATE TABLE `User_PlatformMap` (
    `id` VARCHAR(128) NOT NULL,
    `GameId` BIGINT NOT NULL,
    `PlatformId` BIGINT NOT NULL,
    `Mapping` LONGTEXT,
    PRIMARY KEY (`id`, `GameId`, `PlatformId`),
    CONSTRAINT `User_PlatformMap_UserId` FOREIGN KEY (`id`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
);

ALTER TABLE `UserTimeTracking`
ADD COLUMN `PlatformId` BIGINT,
ADD COLUMN `IsMediaGroup` BOOLEAN DEFAULT FALSE,
ADD COLUMN `RomId` BIGINT,
ADD INDEX `idx_UserTimeTracking_GameId` (`GameId`),
ADD INDEX `idx_UserTimeTracking_UserId` (`UserId`),
ADD INDEX `idx_UserTimeTracking_PlatformId` (`PlatformId`),
ADD INDEX `idx_UserTimeTracking_GameId_UserId_PlatformId` (
    `GameId`,
    `UserId`,
    `PlatformId`
);

CREATE TABLE `User_RecentPlayedRoms` (
    `UserId` varchar(128) NOT NULL,
    `GameId` bigint(20) NOT NULL,
    `PlatformId` bigint(20) NOT NULL,
    `RomId` bigint(20) NOT NULL,
    `IsMediaGroup` tinyint(1) DEFAULT NULL,
    PRIMARY KEY (
        `UserId`,
        `GameId`,
        `PlatformId`
    ),
    CONSTRAINT `RecentPlayedRoms_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `User_GameFavouriteRoms` (
    `UserId` varchar(128) NOT NULL,
    `GameId` bigint(20) NOT NULL,
    `PlatformId` bigint(20) NOT NULL,
    `RomId` bigint(20) NOT NULL,
    `IsMediaGroup` tinyint(1) DEFAULT NULL,
    PRIMARY KEY (
        `UserId`,
        `GameId`,
        `PlatformId`
    ),
    CONSTRAINT `GameFavouriteRoms_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
);

ALTER TABLE `Games_Roms`
CHANGE `Path` `RelativePath` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci DEFAULT NULL;

ALTER TABLE `Games_Roms`
ADD CONSTRAINT Games_Roms_LibraryId FOREIGN KEY (`LibraryId`) REFERENCES `GameLibraries` (`Id`) ON DELETE CASCADE;

CREATE OR REPLACE VIEW view_UserTimeTracking AS
SELECT *, DATE_ADD(
        SessionTime, INTERVAL SessionLength MINUTE
    ) AS SessionEnd
FROM UserTimeTracking;

CREATE INDEX idx_game_name ON Game (`Name`);

CREATE INDEX idx_game_totalratingcount ON Game (TotalRatingCount);

CREATE INDEX idx_alternativename_game ON AlternativeName (Game);

CREATE INDEX idx_gamestate_romid ON GameState (RomId);

CREATE INDEX idx_gamestate_ismediagroup_userid ON GameState (IsMediaGroup, UserId);

CREATE INDEX idx_rommediagroup_gameid ON RomMediaGroup (GameId);

CREATE INDEX idx_favourites_userid_gameid ON Favourites (UserId, GameId);

CREATE TABLE `MetadataMap` (
    `Id` bigint(20) NOT NULL AUTO_INCREMENT,
    `PlatformId` bigint(20) NOT NULL,
    `SignatureGameName` varchar(255) NOT NULL,
    `UserManualLink` varchar(255) DEFAULT NULL,
    `RomCount` int(11) UNSIGNED NOT NULL DEFAULT 0,
    PRIMARY KEY (`id`),
    INDEX `idx_gamename` (
        `SignatureGameName`,
        `PlatformId`
    )
);

CREATE TABLE `MetadataMapBridge` (
    `ParentMapId` bigint(20) NOT NULL,
    `MetadataSourceType` int(11) NOT NULL DEFAULT 0,
    `MetadataSourceId` bigint(20) NOT NULL,
    `Preferred` BOOLEAN NOT NULL DEFAULT 0,
    `ProcessedAtImport` BOOLEAN NOT NULL DEFAULT 0,
    PRIMARY KEY (
        `ParentMapId`,
        `MetadataSourceType`,
        `MetadataSourceId`
    ),
    INDEX `idx_parentmapidpreferred` (`ParentMapId`, `Preferred`),
    INDEX `idx_MetadataSourceType` (`MetadataSourceType`),
    INDEX `idx_MetadataPreferredSource` (
        `MetadataSourceId`,
        `Preferred`
    ),
    INDEX `idx_MetadataMapBridge_MetadataSourceType_MetadataSourceId` (
        `MetadataSourceType`,
        `MetadataSourceId`
    ),
    CONSTRAINT `MetadataMapBridge_MetadataMap` FOREIGN KEY (`ParentMapId`) REFERENCES `MetadataMap` (`Id`) ON DELETE CASCADE
);

CREATE OR REPLACE VIEW `view_MetadataMap` AS
SELECT `MetadataMap`.*, `MetadataMapBridge`.`MetadataSourceType`, `MetadataMapBridge`.`MetadataSourceId`
FROM
    `MetadataMap`
    LEFT JOIN `MetadataMapBridge` ON (
        `MetadataMap`.`Id` = `MetadataMapBridge`.`ParentMapId`
        AND `MetadataMapBridge`.`Preferred` = 1
    );

ALTER TABLE `Games_Roms`
ADD COLUMN `MetadataMapId` BIGINT NOT NULL DEFAULT 0;

-- ALTER TABLE `Games_Roms`
-- ADD CONSTRAINT metadataMapId FOREIGN KEY (`MetadataMapId`) REFERENCES `MetadataMap` (`Id`) ON DELETE CASCADE;

ALTER TABLE `AgeGroup` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `AgeRating` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `AgeRatingContentDescription`
DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `AlternativeName` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Artwork` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Collection` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Company` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `CompanyLogo` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Cover` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `ExternalGame` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Franchise` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Game` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `GameMode` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `GameVideo` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Genre` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `InvolvedCompany` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `MultiplayerMode` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Platform` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `PlatformLogo` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `PlatformVersion` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `PlayerPerspective` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `ReleaseDate` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Screenshot` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Theme` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `GameLocalization` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `Region` DROP COLUMN IF EXISTS `SourceId`;

ALTER TABLE `AgeGroup`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `AgeRating`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `AgeRatingContentDescription`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `AlternativeName`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Artwork`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Collection`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Company`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `CompanyLogo`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Cover`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `ExternalGame`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Franchise`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Game`
CHANGE `Id` `Id` bigint(20) NOT NULL AUTO_INCREMENT,
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP INDEX IF EXISTS `Id_UNIQUE`,
DROP INDEX IF EXISTS `PRIMARY`,
ADD PRIMARY KEY (`Id`, `SourceId`),
ADD INDEX `idx_game_sourceid` (`SourceId` ASC) VISIBLE;

ALTER TABLE `GameMode`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `GameVideo`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Genre`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `InvolvedCompany`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `MultiplayerMode`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Platform`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP INDEX IF EXISTS `Id_UNIQUE`,
DROP INDEX IF EXISTS `PRIMARY`,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `PlatformLogo`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `PlatformVersion`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `PlayerPerspective`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `ReleaseDate`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Screenshot`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Theme`
ADD COLUMN IF NOT EXISTS `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `ReleaseDate`
DROP COLUMN IF EXISTS `Month`,
DROP COLUMN IF EXISTS `Year`,
CHANGE `m` `Month` int(11) DEFAULT NULL,
CHANGE `y` `Year` int(11) DEFAULT NULL;

ALTER TABLE `Games_Roms`
ADD COLUMN `DateCreated` DATETIME DEFAULT CURRENT_TIMESTAMP,
ADD COLUMN `DateUpdated` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
ADD INDEX (`DateCreated`),
ADD INDEX (`DateUpdated`);

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
        `Games_Roms`
        JOIN `GameLibraries` ON (
            `Games_Roms`.`LibraryId` = `GameLibraries`.`Id`
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
    COUNT(`Games_Roms`.`Id`) AS RomCount
FROM (
        (
            `Games_Roms`
            JOIN `view_MetadataMap` ON (
                `view_MetadataMap`.`Id` = `Games_Roms`.`MetadataMapId`
            )
        )
        LEFT JOIN `Game` ON (
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
        LEFT JOIN `AgeGroup` `b` ON (`b`.`GameId` = `a`.`Id`)
    )
ORDER BY `a`.`NameThe`;

DROP TABLE IF EXISTS `GameLocalization`;

CREATE TABLE `GameLocalization` (
    `Checksum` varchar(45) DEFAULT NULL,
    `Cover` BIGINT DEFAULT NULL,
    `CreatedAt` DATETIME DEFAULT NULL,
    `Game` BIGINT DEFAULT NULL,
    `Id` BIGINT NOT NULL,
    `SourceId` INT NOT NULL DEFAULT 1,
    `Name` varchar(255) DEFAULT NULL,
    `Region` BIGINT DEFAULT NULL,
    `UpdatedAt` DATETIME DEFAULT NULL,
    `dateAdded` datetime DEFAULT NULL,
    `lastUpdated` datetime DEFAULT NULL,
    PRIMARY KEY (`Id`),
    FULLTEXT (`Name`),
    INDEX (`Name`)
);

DROP TABLE IF EXISTS `Region`;

CREATE TABLE `Region` (
    `Category` varchar(255) DEFAULT NULL,
    `Checksum` varchar(45) DEFAULT NULL,
    `CreatedAt` DATETIME DEFAULT NULL,
    `Id` BIGINT NOT NULL,
    `SourceId` INT NOT NULL DEFAULT 1,
    `Identifier` varchar(255) DEFAULT NULL,
    `Name` varchar(255) DEFAULT NULL,
    `UpdatedAt` DATETIME DEFAULT NULL,
    `dateAdded` datetime DEFAULT NULL,
    `lastUpdated` datetime DEFAULT NULL,
    PRIMARY KEY (`Id`)
);

CREATE INDEX `idx_GameState_RomId_IsMediaGroup_UserId` ON `GameState` (
    `RomId`,
    `IsMediaGroup`,
    `UserId`
);

CREATE INDEX `idx_RomMediaGroup_GameId_PlatformId` ON `RomMediaGroup` (`GameId`, `PlatformId`);

CREATE INDEX `idx_AlternativeName_Game_SourceId` ON `AlternativeName` (`Game`, `SourceId`);

CREATE INDEX `idx_GameLocalization_Game_SourceId` ON `GameLocalization` (`Game`, `SourceId`);

CREATE INDEX `idx_AgeGroup_GameId_AgeGroupId` ON `AgeGroup` (`GameId`, `AgeGroupId`);

CREATE INDEX `idx_genre_id_name` ON `Genre` (`Id`, `Name`);

CREATE INDEX `idx_theme_id_name` ON `Theme` (`Id`, `Name`);

CREATE INDEX `idx_gamemode_id_name` ON `GameMode` (`Id`, `Name`);

CREATE INDEX `idx_playerperspective_id_name` ON `PlayerPerspective` (`Id`, `Name`);