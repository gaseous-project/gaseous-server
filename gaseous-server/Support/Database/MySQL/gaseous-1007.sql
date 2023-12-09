CREATE TABLE `ClassificationMap` (
  `AgeGroupId` INT NOT NULL,
  `ClassificationBoardId` INT NOT NULL,
  `RatingId` INT NOT NULL,
  PRIMARY KEY (`AgeGroupId`, `ClassificationBoardId`, `RatingId`));

CREATE OR REPLACE VIEW `view_GamesWithRoms` AS
    SELECT DISTINCT
        Games_Roms.GameId AS ROMGameId,
        Game.*,
        CASE
            WHEN
                Game.`Name` LIKE 'The %'
            THEN
                CONCAT(TRIM(SUBSTR(Game.`Name` FROM 4)),
                        ', The')
            ELSE Game.`Name`
        END AS NameThe
    FROM
        Games_Roms
            LEFT JOIN
        Game ON Game.Id = Games_Roms.GameId;

CREATE OR REPLACE VIEW `view_Games` AS
SELECT 
    *
FROM
    (SELECT DISTINCT
        row_number() over (partition by Id order by AgeGroupId desc) as seqnum, view_GamesWithRoms.*,
            (SELECT 
                    AgeGroupId
                FROM
                    ClassificationMap
                WHERE
                    RatingId = AgeRating.Rating
                ORDER BY AgeGroupId DESC) AgeGroupId
    FROM
        view_GamesWithRoms
    LEFT JOIN Relation_Game_AgeRatings ON view_GamesWithRoms.Id = Relation_Game_AgeRatings.GameId
    LEFT JOIN AgeRating ON Relation_Game_AgeRatings.AgeRatingsId = AgeRating.Id
    ) g
WHERE g.seqnum = 1;

CREATE TABLE `ReleaseDate` (
  `Id` BIGINT NOT NULL,
  `Category` INT(11) NULL DEFAULT NULL,
  `Checksum` VARCHAR(45) NULL DEFAULT NULL,
  `CreatedAt` DATETIME NULL DEFAULT NULL,
  `Date` DATETIME NULL,
  `Game` BIGINT NULL,
  `Human` VARCHAR(100) NULL,
  `m` INT NULL,
  `Platform` BIGINT NULL,
  `Region` INT NULL,
  `Status` BIGINT NULL,
  `UpdatedAt` DATETIME NULL DEFAULT NULL,
  `y` INT NULL,
  `dateAdded` DATETIME NULL DEFAULT NULL,
  `lastUpdated` DATETIME NULL DEFAULT NULL,
  PRIMARY KEY (`Id`));

CREATE TABLE `User_Settings` (
  `Id` VARCHAR(128) NOT NULL,
  `Setting` VARCHAR(45) NOT NULL,
  `Value` LONGTEXT NULL DEFAULT NULL,
  PRIMARY KEY (`Id`, `Setting`));

ALTER TABLE `ServerLogs` 
ADD FULLTEXT INDEX `ft_message` USING BTREE (`Message`) VISIBLE;
