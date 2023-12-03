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
