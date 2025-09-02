-- Add lastUpdated columns that auto-update on modification
-- MariaDB/MySQL: use TIMESTAMP with DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP

ALTER TABLE `MetadataMap`
ADD COLUMN `lastUpdated` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;

ALTER TABLE `MetadataMapBridge`
ADD COLUMN `lastUpdated` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;

-- Update parent MetadataMap.lastUpdated when MetadataMapBridge rows are inserted/updated
DROP TRIGGER IF EXISTS `trg_mmb_ai_update_parent_lastUpdated`;
CREATE TRIGGER `trg_mmb_ai_update_parent_lastUpdated`
AFTER INSERT ON `MetadataMapBridge`
FOR EACH ROW
    UPDATE `MetadataMap`
    SET `lastUpdated` = CURRENT_TIMESTAMP
    WHERE `Id` = NEW.`ParentMapId`;

DROP TRIGGER IF EXISTS `trg_mmb_au_update_parent_lastUpdated`;
CREATE TRIGGER `trg_mmb_au_update_parent_lastUpdated`
AFTER UPDATE ON `MetadataMapBridge`
FOR EACH ROW
    UPDATE `MetadataMap`
    SET `lastUpdated` = CURRENT_TIMESTAMP
    WHERE `Id` = NEW.`ParentMapId`;