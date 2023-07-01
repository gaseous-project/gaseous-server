-- MySQL dump 10.13  Distrib 8.0.29, for macos12 (x86_64)
--
-- Host: localhost    Database: gaseous
-- ------------------------------------------------------
-- Server version	8.0.33

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NameS utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `AgeRating`
--

DROP TABLE IF EXISTS `AgeRating`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AgeRating` (
  `Id` bigint NOT NULL,
  `Category` int DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `ContentDescriptions` json DEFAULT NULL,
  `Rating` int DEFAULT NULL,
  `RatingCoverUrl` varchar(255) DEFAULT NULL,
  `Synopsis` longtext,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AgeRatingContentDescription`
--

DROP TABLE IF EXISTS `AgeRatingContentDescription`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AgeRatingContentDescription` (
  `Id` bigint NOT NULL,
  `Category` int DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Description` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AlternativeName`
--

DROP TABLE IF EXISTS `AlternativeName`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AlternativeName` (
  `Id` bigint NOT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Comment` longtext,
  `Game` bigint DEFAULT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Artwork`
--

DROP TABLE IF EXISTS `Artwork`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Artwork` (
  `Id` bigint NOT NULL,
  `AlphaChannel` tinyint(1) DEFAULT NULL,
  `Animated` tinyint(1) DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Game` bigint DEFAULT NULL,
  `Height` int DEFAULT NULL,
  `ImageId` varchar(45) DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `Width` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Collection`
--

DROP TABLE IF EXISTS `Collection`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Collection` (
  `Id` bigint NOT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Games` json DEFAULT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `Slug` varchar(100) DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Company`
--

DROP TABLE IF EXISTS `Company`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Company` (
  `Id` bigint NOT NULL,
  `ChangeDate` datetime DEFAULT NULL,
  `ChangeDateCategory` int DEFAULT NULL,
  `ChangedCompanyId` bigint DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Country` int DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `Description` longtext,
  `Developed` json DEFAULT NULL,
  `Logo` bigint DEFAULT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `Parent` bigint DEFAULT NULL,
  `Published` json DEFAULT NULL,
  `Slug` varchar(100) DEFAULT NULL,
  `StartDate` datetime DEFAULT NULL,
  `StartDateCategory` int DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `Websites` json DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `CompanyLogo`
--

DROP TABLE IF EXISTS `CompanyLogo`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `CompanyLogo` (
  `Id` bigint NOT NULL,
  `AlphaChannel` tinyint(1) DEFAULT NULL,
  `Animated` tinyint(1) DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Height` int DEFAULT NULL,
  `ImageId` varchar(45) DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `Width` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Cover`
--

DROP TABLE IF EXISTS `Cover`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Cover` (
  `Id` bigint NOT NULL,
  `AlphaChannel` tinyint(1) DEFAULT NULL,
  `Animated` tinyint(1) DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Game` bigint DEFAULT NULL,
  `Height` int DEFAULT NULL,
  `ImageId` varchar(45) DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `Width` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ExternalGame`
--

DROP TABLE IF EXISTS `ExternalGame`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ExternalGame` (
  `Id` bigint NOT NULL,
  `Category` int DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `Countries` json DEFAULT NULL,
  `Game` bigint DEFAULT NULL,
  `Media` int DEFAULT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `Platform` bigint DEFAULT NULL,
  `Uid` varchar(255) DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `Year` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Franchise`
--

DROP TABLE IF EXISTS `Franchise`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Franchise` (
  `Id` bigint NOT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `Games` json DEFAULT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `Slug` varchar(255) DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Game`
--

DROP TABLE IF EXISTS `Game`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Game` (
  `Id` bigint NOT NULL,
  `AgeRatings` json DEFAULT NULL,
  `AggregatedRating` double DEFAULT NULL,
  `AggregatedRatingCount` int DEFAULT NULL,
  `AlternativeNames` json DEFAULT NULL,
  `Artworks` json DEFAULT NULL,
  `Bundles` json DEFAULT NULL,
  `Category` int DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Collection` bigint DEFAULT NULL,
  `Cover` bigint DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `Dlcs` json DEFAULT NULL,
  `Expansions` json DEFAULT NULL,
  `ExternalGames` json DEFAULT NULL,
  `FirstReleaseDate` datetime DEFAULT NULL,
  `Follows` int DEFAULT NULL,
  `Franchise` bigint DEFAULT NULL,
  `Franchises` json DEFAULT NULL,
  `GameEngines` json DEFAULT NULL,
  `GameModes` json DEFAULT NULL,
  `Genres` json DEFAULT NULL,
  `Hypes` int DEFAULT NULL,
  `InvolvedCompanies` json DEFAULT NULL,
  `Keywords` json DEFAULT NULL,
  `MultiplayerModes` json DEFAULT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `ParentGame` bigint DEFAULT NULL,
  `Platforms` json DEFAULT NULL,
  `PlayerPerspectives` json DEFAULT NULL,
  `Rating` double DEFAULT NULL,
  `RatingCount` int DEFAULT NULL,
  `ReleaseDates` json DEFAULT NULL,
  `Screenshots` json DEFAULT NULL,
  `SimilarGames` json DEFAULT NULL,
  `Slug` varchar(100) DEFAULT NULL,
  `StandaloneExpansions` json DEFAULT NULL,
  `Status` int DEFAULT NULL,
  `StoryLine` longtext,
  `Summary` longtext,
  `Tags` json DEFAULT NULL,
  `Themes` json DEFAULT NULL,
  `TotalRating` double DEFAULT NULL,
  `TotalRatingCount` int DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `VersionParent` bigint DEFAULT NULL,
  `VersionTitle` varchar(100) DEFAULT NULL,
  `Videos` json DEFAULT NULL,
  `Websites` json DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`),
  KEY `Idx_AgeRatings` ((cast(`AgeRatings` as unsigned array))),
  KEY `Idx_Genres` ((cast(`Genres` as unsigned array))),
  KEY `Idx_alternativeNames` ((cast(`AlternativeNames` as unsigned array))),
  KEY `Idx_artworks` ((cast(`Artworks` as unsigned array))),
  KEY `Idx_bundles` ((cast(`Bundles` as unsigned array))),
  KEY `Idx_dlcs` ((cast(`Dlcs` as unsigned array))),
  KEY `Idx_expansions` ((cast(`Expansions` as unsigned array))),
  KEY `Idx_ExternalGames` ((cast(`ExternalGames` as unsigned array))),
  KEY `Idx_franchises` ((cast(`Franchises` as unsigned array))),
  KEY `Idx_Gameengines` ((cast(`GameEngines` as unsigned array))),
  KEY `Idx_Gamemodes` ((cast(`GameModes` as unsigned array))),
  KEY `Idx_involvedcompanies` ((cast(`InvolvedCompanies` as unsigned array))),
  KEY `Idx_keywords` ((cast(`Keywords` as unsigned array))),
  KEY `Idx_multiplayermodes` ((cast(`MultiplayerModes` as unsigned array))),
  KEY `Idx_Platforms` ((cast(`Platforms` as unsigned array))),
  KEY `Idx_playerperspectives` ((cast(`PlayerPerspectives` as unsigned array))),
  KEY `Idx_releasedates` ((cast(`ReleaseDates` as unsigned array))),
  KEY `Idx_Screenshots` ((cast(`Screenshots` as unsigned array))),
  KEY `Idx_similarGames` ((cast(`SimilarGames` as unsigned array))),
  KEY `Idx_standaloneexpansions` ((cast(`StandaloneExpansions` as unsigned array))),
  KEY `Idx_tags` ((cast(`Tags` as unsigned array))),
  KEY `Idx_themes` ((cast(`Themes` as unsigned array))),
  KEY `Idx_vIdeos` ((cast(`Videos` as unsigned array))),
  KEY `Idx_websites` ((cast(`Websites` as unsigned array)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Games_Roms`
--

DROP TABLE IF EXISTS `Games_Roms`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Games_Roms` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `PlatformId` bigint DEFAULT NULL,
  `GameId` bigint DEFAULT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `Size` bigint DEFAULT NULL,
  `CRC` varchar(20) DEFAULT NULL,
  `MD5` varchar(100) DEFAULT NULL,
  `SHA1` varchar(100) DEFAULT NULL,
  `DevelopmentStatus` varchar(100) DEFAULT NULL,
  `Flags` json DEFAULT NULL,
  `RomType` int DEFAULT NULL,
  `RomTypeMedia` varchar(100) DEFAULT NULL,
  `MediaLabel` varchar(100) DEFAULT NULL,
  `Path` longtext,
  `MetadataSource` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`),
  INDEX `GameId` (`GameId` ASC) VISIBLE,
  INDEX `Id_GameId` (`GameId` ASC, `Id` ASC) VISIBLE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `GameVideo`
--

DROP TABLE IF EXISTS `GameVideo`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `GameVideo` (
  `Id` bigint NOT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Game` bigint DEFAULT NULL,
  `Name` varchar(100) DEFAULT NULL,
  `VideoId` varchar(45) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Genre`
--

DROP TABLE IF EXISTS `Genre`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Genre` (
  `Id` bigint NOT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `Slug` varchar(100) DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `InvolvedCompany`
--

DROP TABLE IF EXISTS `InvolvedCompany`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `InvolvedCompany` (
  `Id` bigint NOT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Company` bigint DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `Developer` tinyint(1) DEFAULT NULL,
  `Game` bigint DEFAULT NULL,
  `Porting` tinyint(1) DEFAULT NULL,
  `Publisher` tinyint(1) DEFAULT NULL,
  `Supporting` tinyint(1) DEFAULT NULL,
  `UpdatedAt` datetime DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Platform`
--

DROP TABLE IF EXISTS `Platform`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Platform` (
  `Id` bigint NOT NULL,
  `Abbreviation` varchar(45) DEFAULT NULL,
  `AlternativeName` varchar(255) DEFAULT NULL,
  `Category` int DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `CreatedAt` datetime DEFAULT NULL,
  `Generation` int DEFAULT NULL,
  `Name` varchar(45) DEFAULT NULL,
  `PlatformFamily` bigint DEFAULT NULL,
  `PlatformLogo` bigint DEFAULT NULL,
  `Slug` varchar(45) DEFAULT NULL,
  `Summary` longtext,
  `UpdatedAt` datetime DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `Versions` json DEFAULT NULL,
  `Websites` json DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PlatformLogo`
--

DROP TABLE IF EXISTS `PlatformLogo`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `PlatformLogo` (
  `Id` bigint NOT NULL,
  `AlphaChannel` tinyint(1) DEFAULT NULL,
  `Animated` tinyint(1) DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Height` int DEFAULT NULL,
  `ImageId` varchar(45) DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `Width` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Platformversion`
--

DROP TABLE IF EXISTS `PlatformVersion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `PlatformVersion` (
  `Id` bigint NOT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Companies` json DEFAULT NULL,
  `Connectivity` longtext,
  `CPU` longtext,
  `Graphics` longtext,
  `MainManufacturer` bigint DEFAULT NULL,
  `Media` longtext,
  `Memory` longtext,
  `Name` longtext,
  `OS` longtext,
  `Output` longtext,
  `PlatformLogo` int DEFAULT NULL,
  `PlatformVersionReleaseDates` json DEFAULT NULL,
  `Resolutions` longtext,
  `Slug` longtext,
  `Sound` longtext,
  `Storage` longtext,
  `Summary` longtext,
  `Url` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Screenshot`
--

DROP TABLE IF EXISTS `Screenshot`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Screenshot` (
  `Id` bigint NOT NULL,
  `AlphaChannel` tinyint(1) DEFAULT NULL,
  `Animated` tinyint(1) DEFAULT NULL,
  `Checksum` varchar(45) DEFAULT NULL,
  `Game` bigint DEFAULT NULL,
  `Height` int DEFAULT NULL,
  `ImageId` varchar(45) DEFAULT NULL,
  `Url` varchar(255) DEFAULT NULL,
  `Width` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Settings`
--

DROP TABLE IF EXISTS `Settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Settings` (
  `Setting` varchar(45) NOT NULL,
  `Value` longtext,
  PRIMARY KEY (`Setting`),
  UNIQUE KEY `Setting_UNIQUE` (`Setting`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Signatures_Games`
--

DROP TABLE IF EXISTS `Signatures_Games`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Signatures_Games` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) DEFAULT NULL,
  `Description` varchar(255) DEFAULT NULL,
  `Year` varchar(15) DEFAULT NULL,
  `PublisherId` int DEFAULT NULL,
  `Demo` int DEFAULT NULL,
  `SystemId` int DEFAULT NULL,
  `SystemVariant` varchar(100) DEFAULT NULL,
  `Video` varchar(10) DEFAULT NULL,
  `Country` varchar(5) DEFAULT NULL,
  `Language` varchar(5) DEFAULT NULL,
  `Copyright` varchar(15) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`),
  KEY `publisher_Idx` (`PublisherId`),
  KEY `system_Idx` (`SystemId`),
  KEY `ingest_Idx` (`Name`,`Year`,`PublisherId`,`SystemId`,`Country`,`Language`) USING BTREE,
  CONSTRAINT `Publisher` FOREIGN KEY (`PublisherId`) REFERENCES `Signatures_Publishers` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `System` FOREIGN KEY (`SystemId`) REFERENCES `Signatures_Platforms` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Signatures_Platforms`
--

DROP TABLE IF EXISTS `Signatures_Platforms`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Signatures_Platforms` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Platform` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IdSignatures_Platforms_UNIQUE` (`Id`),
  KEY `Platforms_Idx` (`Platform`,`Id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Signatures_Publishers`
--

DROP TABLE IF EXISTS `Signatures_Publishers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Signatures_Publishers` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Publisher` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`),
  KEY `publisher_Idx` (`Publisher`,`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Signatures_Roms`
--

DROP TABLE IF EXISTS `Signatures_Roms`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Signatures_Roms` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `GameId` int DEFAULT NULL,
  `Name` varchar(255) DEFAULT NULL,
  `Size` bigint DEFAULT NULL,
  `CRC` varchar(20) DEFAULT NULL,
  `MD5` varchar(100) DEFAULT NULL,
  `SHA1` varchar(100) DEFAULT NULL,
  `DevelopmentStatus` varchar(100) DEFAULT NULL,
  `Flags` json DEFAULT NULL,
  `RomType` int DEFAULT NULL,
  `RomTypeMedia` varchar(100) DEFAULT NULL,
  `MediaLabel` varchar(100) DEFAULT NULL,
  `MetadataSource` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`,`GameId`) USING BTREE,
  KEY `GameId_Idx` (`GameId`),
  KEY `md5_Idx` (`MD5`) USING BTREE,
  KEY `sha1_Idx` (`SHA1`) USING BTREE,
  KEY `flags_Idx` ((cast(`Flags` as char(255) array))),
  CONSTRAINT `GameId` FOREIGN KEY (`GameId`) REFERENCES `Signatures_Games` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Signatures_Sources`
--

DROP TABLE IF EXISTS `Signatures_Sources`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Signatures_Sources` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) DEFAULT NULL,
  `Description` varchar(255) DEFAULT NULL,
  `Category` varchar(45) DEFAULT NULL,
  `Version` varchar(45) DEFAULT NULL,
  `Author` varchar(255) DEFAULT NULL,
  `Email` varchar(45) DEFAULT NULL,
  `Homepage` varchar(45) DEFAULT NULL,
  `Url` varchar(45) DEFAULT NULL,
  `SourceType` varchar(45) DEFAULT NULL,
  `SourceMD5` varchar(45) DEFAULT NULL,
  `SourceSHA1` varchar(45) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`),
  KEY `sourcemd5_Idx` (`SourceMD5`,`Id`) USING BTREE,
  KEY `sourcesha1_Idx` (`SourceSHA1`,`Id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping events for database 'gaseous'
--

--
-- Dumping routines for database 'gaseous'
--

--
-- Final view structure for view `view_Signatures_Games`
--

DROP VIEW IF EXISTS `view_Signatures_Games`;
CREATE VIEW `view_Signatures_Games` AS
    SELECT 
        `Signatures_Games`.`Id` AS `Id`,
        `Signatures_Games`.`Name` AS `Name`,
        `Signatures_Games`.`Description` AS `Description`,
        `Signatures_Games`.`Year` AS `Year`,
        `Signatures_Games`.`PublisherId` AS `PublisherId`,
        `Signatures_Publishers`.`Publisher` AS `Publisher`,
        `Signatures_Games`.`Demo` AS `Demo`,
        `Signatures_Games`.`SystemId` AS `PlatformId`,
        `Signatures_Platforms`.`Platform` AS `Platform`,
        `Signatures_Games`.`SystemVariant` AS `SystemVariant`,
        `Signatures_Games`.`VIdeo` AS `Video`,
        `Signatures_Games`.`Country` AS `Country`,
        `Signatures_Games`.`Language` AS `Language`,
        `Signatures_Games`.`Copyright` AS `Copyright`
    FROM
        ((`Signatures_Games`
        JOIN `Signatures_Publishers` ON ((`Signatures_Games`.`PublisherId` = `Signatures_Publishers`.`Id`)))
        JOIN `Signatures_Platforms` ON ((`Signatures_Games`.`SystemId` = `Signatures_Platforms`.`Id`)));