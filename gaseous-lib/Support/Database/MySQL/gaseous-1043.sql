CREATE OR REPLACE VIEW `view_MetadataMap` AS
SELECT `MetadataMap`.*, `MetadataMapBridge`.`MetadataSourceType`, NULLIF(
        `MetadataMapBridge`.`MetadataSourceId`, -1
    ) AS `MetadataSourceId`
FROM
    `MetadataMap`
    LEFT JOIN `MetadataMapBridge` ON (
        `MetadataMap`.`Id` = `MetadataMapBridge`.`ParentMapId`
        AND `MetadataMapBridge`.`Preferred` = 1
    );

DROP VIEW view_Games_Roms;

CREATE VIEW `view_Games_Roms` AS
select
    `Games_Roms`.`Id` AS `Id`,
    `Games_Roms`.`PlatformId` AS `PlatformId`,
    `view_MetadataMap`.`Id` AS `MetadataMapId`,
    `view_MetadataMap`.`MetadataSourceType` AS `GameIdType`,
    NULLIF(
        `view_MetadataMap`.`MetadataSourceId`,
        -1
    ) AS `GameId`,
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
    `Games_Roms`.`DateHashed` AS `DateHashed`,
    concat(
        `GameLibraries`.`Path`,
        '/',
        `Games_Roms`.`RelativePath`
    ) AS `Path`,
    `GameLibraries`.`Name` AS `LibraryName`
from (
        (
            `Games_Roms`
            join `GameLibraries` on (
                `Games_Roms`.`LibraryId` = `GameLibraries`.`Id`
            )
        )
        left join `view_MetadataMap` on (
            `Games_Roms`.`MetadataMapId` = `view_MetadataMap`.`Id`
        )
    );