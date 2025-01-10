CREATE TABLE `Signatures_RomToSource` (
    `SourceId` int NOT NULL,
    `RomId` int NOT NULL,
    PRIMARY KEY (`SourceId`, `RomId`)
);

CREATE TABLE `Signatures_Games_Countries` (
    `GameId` INT NOT NULL,
    `CountryId` INT NOT NULL,
    PRIMARY KEY (`GameId`, `CountryId`),
    CONSTRAINT `GameCountry` FOREIGN KEY (`GameId`) REFERENCES `Signatures_Games` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
);

CREATE TABLE `Signatures_Games_Languages` (
    `GameId` INT NOT NULL,
    `LanguageId` INT NOT NULL,
    PRIMARY KEY (`GameId`, `LanguageId`),
    CONSTRAINT `GameLanguage` FOREIGN KEY (`GameId`) REFERENCES `Signatures_Games` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
);

ALTER TABLE `Games_Roms` ADD COLUMN `RomDataVersion` INT DEFAULT 1;

CREATE TABLE UserProfiles (
    `Id` VARCHAR(45) NOT NULL,
    `UserId` VARCHAR(45) NOT NULL,
    `DisplayName` VARCHAR(255) NOT NULL,
    `Quip` VARCHAR(255) NOT NULL,
    `Avatar` LONGBLOB,
    `AvatarExtension` CHAR(6),
    `ProfileBackground` LONGBLOB,
    `ProfileBackgroundExtension` CHAR(6),
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
ADD COLUMN `RomId` BIGINT;

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

CREATE VIEW view_Games_Roms AS
SELECT `Games_Roms`.*, CONCAT(
        `GameLibraries`.`Path`, '/', `Games_Roms`.`RelativePath`
    ) AS `Path`, `GameLibraries`.`Name` AS `LibraryName`
FROM
    `Games_Roms`
    JOIN `GameLibraries` ON `Games_Roms`.`LibraryId` = `GameLibraries`.`Id`;

CREATE VIEW view_UserTimeTracking AS
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

ALTER TABLE `Games_Roms`
ADD CONSTRAINT metadataMapId FOREIGN KEY (`MetadataMapId`) REFERENCES `MetadataMap` (`Id`) ON DELETE CASCADE;

ALTER TABLE `AgeGroup`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `AgeRating`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `AgeRatingContentDescription`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `AlternativeName`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Artwork`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Collection`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Company`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `CompanyLogo`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Cover`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `ExternalGame`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Franchise`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Game`
CHANGE `Id` `Id` bigint(20) NOT NULL AUTO_INCREMENT,
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP INDEX IF EXISTS `Id_UNIQUE`,
DROP INDEX IF EXISTS `PRIMARY`,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `GameMode`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `GameVideo`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Genre`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `InvolvedCompany`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `MultiplayerMode`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Platform`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP INDEX IF EXISTS `Id_UNIQUE`,
DROP INDEX IF EXISTS `PRIMARY`,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `PlatformLogo`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `PlatformVersion`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `PlayerPerspective`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `ReleaseDate`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Screenshot`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `Theme`
ADD COLUMN `SourceId` INT NOT NULL DEFAULT 1 AFTER `Id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY (`Id`, `SourceId`);

ALTER TABLE `ReleaseDate`
CHANGE `m` `Month` int(11) DEFAULT NULL,
CHANGE `y` `Year` int(11) DEFAULT NULL;

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
    `Game`.`lastUpdated` AS `lastUpdated`
FROM (
        `Games_Roms`
        JOIN `view_MetadataMap` ON (
            `view_MetadataMap`.`Id` = `Games_Roms`.`MetadataMapId`
        )
        LEFT JOIN `Game` ON (
            `Game`.`SourceId` = `view_MetadataMap`.`MetadataSourceType`
            AND `Game`.`Id` = `view_MetadataMap`.`MetadataSourceId`
        )
    );
