ALTER TABLE `Signatures_Games`
CHANGE `Year` `Year` varchar(50) DEFAULT NULL;

ALTER TABLE `Signatures_Roms`
CHANGE `MediaLabel` `MediaLabel` varchar(255) DEFAULT NULL;