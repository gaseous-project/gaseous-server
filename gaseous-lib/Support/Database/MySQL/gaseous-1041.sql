ALTER TABLE `PlatformMap`
ADD COLUMN `AdditionalFiles` LONGTEXT DEFAULT "{}";

ALTER TABLE `MetadataMap`
CHANGE `SignatureGameNameThe` `SignatureGameNameThe` varchar(256) DEFAULT NULL;