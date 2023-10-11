CREATE TABLE `GameLibraries` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(255) NOT NULL,
  `Path` longtext NOT NULL,
  `DefaultLibrary` int NOT NULL DEFAULT '0',
  `DefaultPlatform` bigint NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`)
);

ALTER TABLE `Games_Roms` 
ADD COLUMN `LibraryId` INT NULL DEFAULT 0 AFTER `MetadataVersion`;