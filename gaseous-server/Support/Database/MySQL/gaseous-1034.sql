ALTER TABLE `gaseous`.`ServerLogs`
ADD COLUMN `AdditionalData` LONGTEXT NULL DEFAULT NULL AFTER `Message`;