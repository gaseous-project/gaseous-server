CREATE TABLE `ClearLogo` (
    `Id` bigint(20) NOT NULL,
    `SourceId` int(11) NOT NULL DEFAULT 1,
    `AlphaChannel` tinyint(1) DEFAULT NULL,
    `Animated` tinyint(1) DEFAULT NULL,
    `Checksum` varchar(45) DEFAULT NULL,
    `Game` bigint(20) DEFAULT NULL,
    `Height` int(11) DEFAULT NULL,
    `ImageId` varchar(45) DEFAULT NULL,
    `Url` varchar(255) DEFAULT NULL,
    `Width` int(11) DEFAULT NULL,
    `dateAdded` datetime DEFAULT NULL,
    `lastUpdated` datetime DEFAULT NULL,
    PRIMARY KEY (`Id`, `SourceId`)
);