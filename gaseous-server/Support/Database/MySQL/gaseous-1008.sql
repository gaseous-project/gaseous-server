ALTER TABLE `ClassificationMap` 
ADD INDEX `idx_RatingId` (`RatingId` ASC) VISIBLE;

ALTER TABLE `Relation_Game_AgeRatings` 
ADD INDEX `idx_SecondaryColumn` (`AgeRatingsId` ASC) VISIBLE;

ALTER TABLE `Relation_Game_GameModes` 
ADD INDEX `idx_SecondaryColumn` (`GameModesId` ASC) VISIBLE;

ALTER TABLE `Relation_Game_Genres` 
ADD INDEX `idx_SecondaryColumn` (`GenresId` ASC) VISIBLE;

ALTER TABLE `Relation_Game_Platforms` 
ADD INDEX `idx_SecondaryColumn` (`PlatformsId` ASC) VISIBLE;

ALTER TABLE `Relation_Game_PlayerPerspectives` 
ADD INDEX `idx_SecondaryColumn` (`PlayerPerspectivesId` ASC) VISIBLE;

ALTER TABLE `Relation_Game_Themes` 
ADD INDEX `idx_SecondaryColumn` (`ThemesId` ASC) VISIBLE;