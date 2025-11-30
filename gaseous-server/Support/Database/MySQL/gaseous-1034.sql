DELETE FROM `gaseous`.`ServerLogs`
WHERE `EventTime` < NOW() - INTERVAL 2 DAY;

ALTER TABLE `gaseous`.`ServerLogs`
ADD COLUMN `AdditionalData` LONGTEXT NULL DEFAULT NULL AFTER `Message`;