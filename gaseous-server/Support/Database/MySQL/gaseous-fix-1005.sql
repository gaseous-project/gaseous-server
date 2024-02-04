CREATE TABLE `Relation_Game_AgeRatings` (
  `GameId` BIGINT NOT NULL,
  `AgeRatingsId` BIGINT NOT NULL,
  PRIMARY KEY (`GameId`, `AgeRatingsId`),
  INDEX `idx_PrimaryColumn` (`GameId` ASC) VISIBLE
);

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

CREATE TABLE `RomMediaGroup` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Status` INT NULL,
  `PlatformId` BIGINT NULL,
  `GameId` BIGINT NULL,
  PRIMARY KEY (`Id`));

CREATE TABLE `RomMediaGroup_Members` (
  `GroupId` BIGINT NOT NULL,
  `RomId` BIGINT NOT NULL,
  PRIMARY KEY (`GroupId`, `RomId`));
