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

CREATE TABLE `Relation_Game_Genres` (
  `GameId` BIGINT NOT NULL,
  `GenresId` BIGINT NOT NULL,
  PRIMARY KEY (`GameId`, `GenresId`),
  INDEX `idx_PrimaryColumn` (`GameId` ASC) VISIBLE
);

CREATE TABLE `Relation_Game_GameModes` (
  `GameId` BIGINT NOT NULL,
  `GameModesId` BIGINT NOT NULL,
  PRIMARY KEY (`GameId`, `GameModesId`),
  INDEX `idx_PrimaryColumn` (`GameId` ASC) VISIBLE
);

CREATE TABLE `Relation_Game_PlayerPerspectives` (
  `GameId` BIGINT NOT NULL,
  `PlayerPerspectivesId` BIGINT NOT NULL,
  PRIMARY KEY (`GameId`, `PlayerPerspectivesId`),
  INDEX `idx_PrimaryColumn` (`GameId` ASC) VISIBLE
);

CREATE TABLE `Relation_Game_Themes` (
  `GameId` BIGINT NOT NULL,
  `ThemesId` BIGINT NOT NULL,
  PRIMARY KEY (`GameId`, `ThemesId`),
  INDEX `idx_PrimaryColumn` (`GameId` ASC) VISIBLE
);

ALTER TABLE `Games_Roms` 
ADD COLUMN `LastMatchAttemptDate` DATETIME NULL AFTER `LibraryId`;