ALTER TABLE `Games_Roms`
ADD COLUMN `OriginalFileName` VARCHAR(255) DEFAULT NULL AFTER `Name`;

UPDATE `Games_Roms` SET `OriginalFileName` = `Name`;