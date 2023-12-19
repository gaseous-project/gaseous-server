CREATE TABLE `AgeGroup` (
  `Id` BIGINT NOT NULL,
  `GameId` BIGINT NULL,
  `AgeGroupId` INT NULL,
  `dateAdded` DATETIME NULL DEFAULT NULL,
  `lastUpdated` DATETIME NULL DEFAULT NULL,
  PRIMARY KEY (`Id`));

ALTER TABLE `Game` 
CHANGE COLUMN `Slug` `Slug` VARCHAR(255) NULL DEFAULT NULL;

CREATE OR REPLACE VIEW `view_Games` AS
SELECT 
    a.*, b.AgeGroupId
FROM
    view_GamesWithRoms a
    LEFT JOIN AgeGroup b ON b.GameId = a.Id
ORDER BY NameThe;