ALTER TABLE `Games_Roms`
ADD COLUMN `SHA256` VARCHAR(64) NULL DEFAULT NULL AFTER `SHA1`,
ADD INDEX `SHA256` (`SHA256`),
ADD INDEX `SHA1` (`SHA1`),
ADD INDEX `CRC` (`CRC`);

ALTER TABLE `Signatures_Roms`
ADD COLUMN `SHA256` VARCHAR(64) NULL DEFAULT NULL AFTER `SHA1`,
ADD INDEX `SHA256` (`SHA256`),
ADD INDEX `CRC` (`CRC`);

CREATE
OR
REPLACE
    ALGORITHM = UNDEFINED DEFINER = `root` @`%` SQL SECURITY DEFINER VIEW `view_Games_Roms` AS
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