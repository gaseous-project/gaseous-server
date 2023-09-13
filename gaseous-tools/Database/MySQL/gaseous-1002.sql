ALTER TABLE `Signatures_Roms`
DROP INDEX `flags_Idx`;

ALTER TABLE `Signatures_Roms` 
ADD COLUMN `Attributes` JSON NULL AFTER `Flags`,
ADD COLUMN `IngestorVersion` INT NULL DEFAULT 1;

ALTER TABLE `Games_Roms` 
ADD COLUMN `Attributes` JSON NULL AFTER `Flags`,
ADD COLUMN `MetadataGameName` VARCHAR(255) NULL AFTER `MetadataSource`,
ADD COLUMN `MetadataVersion` INT NULL DEFAULT 1;

ALTER TABLE `RomCollections` 
ADD COLUMN `FolderStructure` INT NULL DEFAULT 0 AFTER `MaximumCollectionSizeInBytes`,
ADD COLUMN `IncludeBIOSFiles` BOOLEAN NULL DEFAULT 0 AFTER `FolderStructure`,
ADD COLUMN `AlwaysInclude` JSON NULL AFTER `IncludeBIOSFiles`;
