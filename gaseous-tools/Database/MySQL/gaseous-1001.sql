DROP TABLE IF EXISTS `GameMode`;
CREATE TABLE `gaseous`.`GameMode` (
  `Id` BIGINT NOT NULL,
  `CreatedAt` DATETIME NULL,
  `Checksum` VARCHAR(45) NULL,
  `Name` VARCHAR(100) NULL,
  `Slug` VARCHAR(100) NULL,
  `UpdatedAt` DATETIME NULL,
  `Url` VARCHAR(255) NULL,
  `dateAdded` DATETIME NULL,
  `lastUpdated` DATETIME NULL,
  PRIMARY KEY (`Id`)
);

DROP TABLE IF EXISTS `MultiplayerMode`;
CREATE TABLE `MultiplayerMode` (
  `Id` bigint NOT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `CampaignCoop` int DEFAULT NULL,
  `DropIn` int DEFAULT NULL,
  `Game` bigint DEFAULT NULL,
  `LanCoop` int DEFAULT NULL,
  `OfflineCoop` int DEFAULT NULL,
  `OfflineCoopMax` int DEFAULT NULL,
  `OfflineMax` int DEFAULT NULL,
  `OnlineCoop` int DEFAULT NULL,
  `OnlineCoopMax` int DEFAULT NULL,
  `OnlineMax` int DEFAULT NULL,
  `Platform` bigint DEFAULT NULL,
  `SplitScreen` int DEFAULT NULL,
  `SplitScreenOnline` int DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
);