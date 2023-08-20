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
  `CampaignCoop` boolean DEFAULT NULL,
  `DropIn` boolean DEFAULT NULL,
  `Game` bigint DEFAULT NULL,
  `LanCoop` boolean DEFAULT NULL,
  `OfflineCoop` boolean DEFAULT NULL,
  `OfflineCoopMax` int DEFAULT NULL,
  `OfflineMax` int DEFAULT NULL,
  `OnlineCoop` boolean DEFAULT NULL,
  `OnlineCoopMax` int DEFAULT NULL,
  `OnlineMax` int DEFAULT NULL,
  `Platform` bigint DEFAULT NULL,
  `SplitScreen` boolean DEFAULT NULL,
  `SplitScreenOnline` boolean DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
);

DROP TABLE IF EXISTS `PlayerPerspective`;
CREATE TABLE `PlayerPerspective` (
  `Id` bigint NOT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Name` varchar(100) DEFAULT NULL,
  `Slug` varchar(45) DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
);

DROP TABLE IF EXISTS `Theme`;
CREATE TABLE `Theme` (
  `Id` bigint NOT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Name` varchar(100) DEFAULT NULL,
  `Slug` varchar(45) DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
);

DROP TABLE IF EXISTS `RomCollections`;
CREATE TABLE `RomCollections` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(255) NULL,
  `Description` LONGTEXT NULL,
  `Platforms` JSON NULL,
  `Genres` JSON NULL,
  `Players` JSON NULL,
  `PlayerPerspectives` JSON NULL,
  `Themes` JSON NULL,
  `MinimumRating` INT NULL,
  `MaximumRating` INT NULL,
  `MaximumRomsPerPlatform` INT NULL,
  `MaximumBytesPerPlatform` BIGINT NULL,
  `MaximumCollectionSizeInBytes` BIGINT NULL,
  `BuiltStatus` INT NULL,
  PRIMARY KEY (`Id`));
