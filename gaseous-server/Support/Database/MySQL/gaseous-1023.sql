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

CREATE TABLE `Country` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Code` VARCHAR(20) NULL,
    `Value` VARCHAR(255) NULL,
    PRIMARY KEY (`Id`),
    INDEX `id_Code` (`Code` ASC) VISIBLE,
    INDEX `id_Value` (`Value` ASC) VISIBLE
);

CREATE TABLE `Language` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Code` VARCHAR(20) NULL,
    `Value` VARCHAR(255) NULL,
    PRIMARY KEY (`Id`),
    INDEX `id_Code` (`Code` ASC) VISIBLE,
    INDEX `id_Value` (`Value` ASC) VISIBLE
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