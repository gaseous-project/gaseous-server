ALTER TABLE `Metadata_Game`
ADD COLUMN `ClearLogo` LONGTEXT,
ADD COLUMN `MetadataSource` INT NOT NULL DEFAULT 0 AFTER `SourceId`;

RENAME TABLE `Metadata_AgeRatingContentDescriptionV2` TO `Metadata_AgeRatingContentDescription`;