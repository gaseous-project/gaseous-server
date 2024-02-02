ALTER TABLE `Collection` 
ADD COLUMN `AsParentRelations` LONGTEXT NULL AFTER `Id`,
ADD COLUMN `AsChildRelations` LONGTEXT NULL AFTER `AsParentRelations`,
ADD COLUMN `Type` INT NULL AFTER `UpdatedAt`;

ALTER TABLE `Cover` 
ADD COLUMN `GameLocalization` BIGINT NULL AFTER `Game`;

ALTER TABLE `Game`
ADD COLUMN `Collections` LONGTEXT NULL AFTER `Collection`,
ADD COLUMN `ExpandedGames` LONGTEXT NULL AFTER `Dlcs`,
ADD COLUMN `Forks` LONGTEXT NULL AFTER `Follows`,
ADD COLUMN `GameLocalizations` LONGTEXT NULL AFTER `GameEngines`,
ADD COLUMN `LanguageSupports` LONGTEXT NULL AFTER `Keywords`,
ADD COLUMN `Ports` LONGTEXT NULL AFTER `PlayerPerspectives`,
ADD COLUMN `Remakes` LONGTEXT NULL AFTER `ReleaseDates`,
ADD COLUMN `Remasters` LONGTEXT NULL AFTER `Remakes`;