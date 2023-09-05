ALTER TABLE `Signatures_Roms`
DROP INDEX `flags_Idx`;

ALTER TABLE `Signatures_Roms` 
ADD COLUMN `Attributes` JSON NULL AFTER `Flags`,
ADD COLUMN `IngestorVersion` INT NULL DEFAULT 1;

ALTER TABLE `Games_Roms` 
ADD COLUMN `Attributes` JSON NULL AFTER `Flags`,
ADD COLUMN `MetadataGameName` VARCHAR(255) NULL AFTER `MetadataSource`,
ADD COLUMN `MetadataVersion` INT NULL DEFAULT 1;