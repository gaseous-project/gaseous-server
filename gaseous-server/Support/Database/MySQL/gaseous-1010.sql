CREATE OR REPLACE VIEW `view_Games` AS
SELECT 
    a.*, b.AgeGroupId
FROM
    view_GamesWithRoms a
        INNER JOIN
    (SELECT 
        view_GamesWithRoms.Id,
            MAX((SELECT 
                    AgeGroupId
                FROM
                    ClassificationMap
                WHERE
                    RatingId = AgeRating.Rating)) AgeGroupId
    FROM
        view_GamesWithRoms
    LEFT JOIN Relation_Game_AgeRatings ON view_GamesWithRoms.Id = Relation_Game_AgeRatings.GameId
    LEFT JOIN AgeRating ON Relation_Game_AgeRatings.AgeRatingsId = AgeRating.Id
    GROUP BY Id) b ON a.Id = b.Id
ORDER BY NameThe;

ALTER TABLE `ServerLogs` 
ADD COLUMN `CallingUser` VARCHAR(255) NULL AFTER `CallingProcess`;