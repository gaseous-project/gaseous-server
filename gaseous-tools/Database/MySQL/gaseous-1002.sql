ALTER TABLE `Signatures_Roms`
DROP INDEX `flags_Idx`;

ALTER TABLE `Signatures_Roms` 
ADD COLUMN `Attributes` JSON NULL AFTER `Flags`,
ADD COLUMN `IngestorVersion` INT NULL DEFAULT 1;

ALTER TABLE `Games_Roms` 
ADD COLUMN `Attributes` JSON NULL AFTER `Flags`,
ADD COLUMN `MetadataGameName` VARCHAR(255) NULL AFTER `MetadataSource`,
ADD COLUMN `MetadataVersion` INT NULL DEFAULT 1;

ALTER TABLE `RomCollections` 
ADD COLUMN `FolderStructure` INT NULL DEFAULT 0 AFTER `MaximumCollectionSizeInBytes`,
ADD COLUMN `IncludeBIOSFiles` BOOLEAN NULL DEFAULT 0 AFTER `FolderStructure`,
ADD COLUMN `AlwaysInclude` JSON NULL AFTER `IncludeBIOSFiles`;

CREATE TABLE `PlatformMap` (
  `Id` BIGINT NOT NULL,
  `RetroPieDirectoryName` VARCHAR(45) NULL,
  `WebEmulator_Type` VARCHAR(45) NULL,
  `WebEmulator_Core` VARCHAR(45) NULL,
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `Id_UNIQUE` (`Id` ASC) VISIBLE);

CREATE TABLE `PlatformMap_AlternateNames` (
  `Id` BIGINT NOT NULL,
  `Name` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`Id`, `Name`));

CREATE TABLE `PlatformMap_Extensions` (
  `Id` BIGINT NOT NULL,
  `Extension` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`, `Extension`));

CREATE TABLE `PlatformMap_UniqueExtensions` (
  `Id` BIGINT NOT NULL,
  `Extension` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`, `Extension`));

CREATE TABLE `PlatformMap_Bios` (
  `Id` BIGINT NOT NULL,
  `Filename` VARCHAR(45) NOT NULL,
  `Description` LONGTEXT NOT NULL,
  `Hash` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`, `Filename`, `Hash`));

CREATE TABLE `ServerLogs` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `EventTime` DATETIME NOT NULL,
  `EventType` INT NOT NULL,
  `Process` VARCHAR(100) NOT NULL,
  `Message` LONGTEXT NOT NULL,
  `Exception` LONGTEXT NULL,
  PRIMARY KEY (`Id`));

ALTER TABLE `PlatformVersion` 
CHANGE COLUMN `PlatformLogo` `PlatformLogo` BIGINT NULL DEFAULT NULL ;