-- MySQL dump 10.13  Distrib 8.0.29, for macos12 (x86_64)
--
-- Host: localhost    Database: gaseous
-- ------------------------------------------------------
-- Server version	8.0.33

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `agerating`
--

DROP TABLE IF EXISTS `agerating`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `agerating` (
  `id` bigint NOT NULL,
  `category` int DEFAULT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `contentdescriptions` json DEFAULT NULL,
  `rating` int DEFAULT NULL,
  `ratingcoverurl` varchar(255) DEFAULT NULL,
  `synopsis` longtext,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ageratingcontentdescription`
--

DROP TABLE IF EXISTS `ageratingcontentdescription`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ageratingcontentdescription` (
  `id` bigint NOT NULL,
  `category` int DEFAULT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `alternativename`
--

DROP TABLE IF EXISTS `alternativename`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `alternativename` (
  `id` bigint NOT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `comment` longtext,
  `game` bigint DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `artwork`
--

DROP TABLE IF EXISTS `artwork`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `artwork` (
  `id` bigint NOT NULL,
  `alphachannel` tinyint(1) DEFAULT NULL,
  `animated` tinyint(1) DEFAULT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `game` bigint DEFAULT NULL,
  `height` int DEFAULT NULL,
  `imageid` varchar(45) DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `width` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `collection`
--

DROP TABLE IF EXISTS `collection`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `collection` (
  `id` bigint NOT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `games` json DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  `slug` varchar(100) DEFAULT NULL,
  `createdAt` datetime DEFAULT NULL,
  `updatedAt` datetime DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `cover`
--

DROP TABLE IF EXISTS `cover`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cover` (
  `id` bigint NOT NULL,
  `alphachannel` tinyint(1) DEFAULT NULL,
  `animated` tinyint(1) DEFAULT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `game` bigint DEFAULT NULL,
  `height` int DEFAULT NULL,
  `imageid` varchar(45) DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `width` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `externalgame`
--

DROP TABLE IF EXISTS `externalgame`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `externalgame` (
  `id` bigint NOT NULL,
  `category` int DEFAULT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `createdat` datetime DEFAULT NULL,
  `countries` json DEFAULT NULL,
  `game` bigint DEFAULT NULL,
  `media` int DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  `platform` bigint DEFAULT NULL,
  `uid` varchar(255) DEFAULT NULL,
  `updatedat` datetime DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `year` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `franchise`
--

DROP TABLE IF EXISTS `franchise`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `franchise` (
  `id` bigint NOT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `createdat` datetime DEFAULT NULL,
  `updatedat` datetime DEFAULT NULL,
  `games` json DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  `slug` varchar(255) DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `game`
--

DROP TABLE IF EXISTS `game`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `game` (
  `id` bigint NOT NULL,
  `ageratings` json DEFAULT NULL,
  `aggregatedrating` double DEFAULT NULL,
  `aggregatedratingcount` int DEFAULT NULL,
  `alternativenames` json DEFAULT NULL,
  `artworks` json DEFAULT NULL,
  `bundles` json DEFAULT NULL,
  `category` int DEFAULT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `collection` bigint DEFAULT NULL,
  `cover` bigint DEFAULT NULL,
  `createdat` datetime DEFAULT NULL,
  `dlcs` json DEFAULT NULL,
  `expansions` json DEFAULT NULL,
  `externalgames` json DEFAULT NULL,
  `firstreleasedate` datetime DEFAULT NULL,
  `follows` int DEFAULT NULL,
  `franchise` bigint DEFAULT NULL,
  `franchises` json DEFAULT NULL,
  `gameengines` json DEFAULT NULL,
  `gamemodes` json DEFAULT NULL,
  `genres` json DEFAULT NULL,
  `hypes` int DEFAULT NULL,
  `involvedcompanies` json DEFAULT NULL,
  `keywords` json DEFAULT NULL,
  `multiplayermodes` json DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  `parentgame` bigint DEFAULT NULL,
  `platforms` json DEFAULT NULL,
  `playerperspectives` json DEFAULT NULL,
  `rating` double DEFAULT NULL,
  `ratingcount` int DEFAULT NULL,
  `releasedates` json DEFAULT NULL,
  `screenshots` json DEFAULT NULL,
  `similargames` json DEFAULT NULL,
  `slug` varchar(100) DEFAULT NULL,
  `standaloneexpansions` json DEFAULT NULL,
  `status` int DEFAULT NULL,
  `storyline` longtext,
  `summary` longtext,
  `tags` json DEFAULT NULL,
  `themes` json DEFAULT NULL,
  `totalrating` double DEFAULT NULL,
  `totalratingcount` int DEFAULT NULL,
  `updatedat` datetime DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `versionparent` bigint DEFAULT NULL,
  `versiontitle` varchar(100) DEFAULT NULL,
  `videos` json DEFAULT NULL,
  `websites` json DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`),
  KEY `idx_genres` ((cast(`genres` as unsigned array))),
  KEY `idx_alternativenames` ((cast(`alternativenames` as unsigned array))),
  KEY `idx_artworks` ((cast(`artworks` as unsigned array))),
  KEY `idx_bundles` ((cast(`bundles` as unsigned array))),
  KEY `idx_dlcs` ((cast(`dlcs` as unsigned array))),
  KEY `idx_expansions` ((cast(`expansions` as unsigned array))),
  KEY `idx_externalgames` ((cast(`externalgames` as unsigned array))),
  KEY `idx_franchises` ((cast(`franchises` as unsigned array))),
  KEY `idx_gameengines` ((cast(`gameengines` as unsigned array))),
  KEY `idx_gamemodes` ((cast(`gamemodes` as unsigned array))),
  KEY `idx_involvedcompanies` ((cast(`involvedcompanies` as unsigned array))),
  KEY `idx_keywords` ((cast(`keywords` as unsigned array))),
  KEY `idx_multiplayermodes` ((cast(`multiplayermodes` as unsigned array))),
  KEY `idx_platforms` ((cast(`platforms` as unsigned array))),
  KEY `idx_playerperspectives` ((cast(`playerperspectives` as unsigned array))),
  KEY `idx_releasedates` ((cast(`releasedates` as unsigned array))),
  KEY `idx_screenshots` ((cast(`screenshots` as unsigned array))),
  KEY `idx_similargames` ((cast(`similargames` as unsigned array))),
  KEY `idx_standaloneexpansions` ((cast(`standaloneexpansions` as unsigned array))),
  KEY `idx_tags` ((cast(`tags` as unsigned array))),
  KEY `idx_themes` ((cast(`themes` as unsigned array))),
  KEY `idx_videos` ((cast(`videos` as unsigned array))),
  KEY `idx_websites` ((cast(`websites` as unsigned array)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `games_roms`
--

DROP TABLE IF EXISTS `games_roms`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `games_roms` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `platformid` bigint DEFAULT NULL,
  `gameid` bigint DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  `size` bigint DEFAULT NULL,
  `crc` varchar(20) DEFAULT NULL,
  `md5` varchar(100) DEFAULT NULL,
  `sha1` varchar(100) DEFAULT NULL,
  `developmentstatus` varchar(100) DEFAULT NULL,
  `flags` json DEFAULT NULL,
  `romtype` int DEFAULT NULL,
  `romtypemedia` varchar(100) DEFAULT NULL,
  `medialabel` varchar(100) DEFAULT NULL,
  `path` longtext,
  `metadatasource` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `gamevideo`
--

DROP TABLE IF EXISTS `gamevideo`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `gamevideo` (
  `id` bigint NOT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `game` bigint DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `videoid` varchar(45) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `genre`
--

DROP TABLE IF EXISTS `genre`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `genre` (
  `id` bigint NOT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `createdat` datetime DEFAULT NULL,
  `updatedat` datetime DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  `slug` varchar(100) DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `platform`
--

DROP TABLE IF EXISTS `platform`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `platform` (
  `id` bigint NOT NULL,
  `abbreviation` varchar(45) DEFAULT NULL,
  `alternativename` varchar(255) DEFAULT NULL,
  `category` int DEFAULT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `createdat` datetime DEFAULT NULL,
  `generation` int DEFAULT NULL,
  `name` varchar(45) DEFAULT NULL,
  `platformfamily` bigint DEFAULT NULL,
  `platformlogo` bigint DEFAULT NULL,
  `slug` varchar(45) DEFAULT NULL,
  `summary` longtext,
  `updatedat` datetime DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `versions` json DEFAULT NULL,
  `websites` json DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `platformlogo`
--

DROP TABLE IF EXISTS `platformlogo`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `platformlogo` (
  `id` bigint NOT NULL,
  `alphachannel` tinyint(1) DEFAULT NULL,
  `animated` tinyint(1) DEFAULT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `height` int DEFAULT NULL,
  `imageid` varchar(45) DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `width` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `platformversion`
--

DROP TABLE IF EXISTS `platformversion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `platformversion` (
  `id` bigint NOT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `companies` json DEFAULT NULL,
  `connectivity` longtext,
  `cpu` longtext,
  `graphics` longtext,
  `mainmanufacturer` bigint DEFAULT NULL,
  `media` longtext,
  `memory` longtext,
  `name` longtext,
  `os` longtext,
  `output` longtext,
  `platformlogo` int DEFAULT NULL,
  `platformversionreleasedates` json DEFAULT NULL,
  `resolutions` longtext,
  `slug` longtext,
  `sound` longtext,
  `storage` longtext,
  `summary` longtext,
  `url` varchar(255) DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `screenshot`
--

DROP TABLE IF EXISTS `screenshot`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `screenshot` (
  `id` bigint NOT NULL,
  `alphachannel` tinyint(1) DEFAULT NULL,
  `animated` tinyint(1) DEFAULT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `game` bigint DEFAULT NULL,
  `height` int DEFAULT NULL,
  `imageid` varchar(45) DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `width` int DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `settings`
--

DROP TABLE IF EXISTS `settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `settings` (
  `setting` varchar(45) NOT NULL,
  `value` longtext,
  PRIMARY KEY (`setting`),
  UNIQUE KEY `setting_UNIQUE` (`setting`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `signatures_games`
--

DROP TABLE IF EXISTS `signatures_games`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `signatures_games` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `year` varchar(15) DEFAULT NULL,
  `publisherid` int DEFAULT NULL,
  `demo` int DEFAULT NULL,
  `systemid` int DEFAULT NULL,
  `systemvariant` varchar(100) DEFAULT NULL,
  `video` varchar(10) DEFAULT NULL,
  `country` varchar(5) DEFAULT NULL,
  `language` varchar(5) DEFAULT NULL,
  `copyright` varchar(15) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`),
  KEY `publisher_idx` (`publisherid`),
  KEY `system_idx` (`systemid`),
  KEY `ingest_idx` (`name`,`year`,`publisherid`,`systemid`,`country`,`language`) USING BTREE,
  CONSTRAINT `publisher` FOREIGN KEY (`publisherid`) REFERENCES `signatures_publishers` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `system` FOREIGN KEY (`systemid`) REFERENCES `signatures_platforms` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `signatures_platforms`
--

DROP TABLE IF EXISTS `signatures_platforms`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `signatures_platforms` (
  `id` int NOT NULL AUTO_INCREMENT,
  `platform` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `idsignatures_platforms_UNIQUE` (`id`),
  KEY `platforms_idx` (`platform`,`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `signatures_publishers`
--

DROP TABLE IF EXISTS `signatures_publishers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `signatures_publishers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `publisher` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`),
  KEY `publisher_idx` (`publisher`,`id`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `signatures_roms`
--

DROP TABLE IF EXISTS `signatures_roms`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `signatures_roms` (
  `id` int NOT NULL AUTO_INCREMENT,
  `gameid` int DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  `size` bigint DEFAULT NULL,
  `crc` varchar(20) DEFAULT NULL,
  `md5` varchar(100) DEFAULT NULL,
  `sha1` varchar(100) DEFAULT NULL,
  `developmentstatus` varchar(100) DEFAULT NULL,
  `flags` json DEFAULT NULL,
  `romtype` int DEFAULT NULL,
  `romtypemedia` varchar(100) DEFAULT NULL,
  `medialabel` varchar(100) DEFAULT NULL,
  `metadatasource` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`,`gameid`) USING BTREE,
  KEY `gameid_idx` (`gameid`),
  KEY `md5_idx` (`md5`) USING BTREE,
  KEY `sha1_idx` (`sha1`) USING BTREE,
  KEY `flags_idx` ((cast(`flags` as char(255) array))),
  CONSTRAINT `gameid` FOREIGN KEY (`gameid`) REFERENCES `signatures_games` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `signatures_sources`
--

DROP TABLE IF EXISTS `signatures_sources`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `signatures_sources` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `category` varchar(45) DEFAULT NULL,
  `version` varchar(45) DEFAULT NULL,
  `author` varchar(255) DEFAULT NULL,
  `email` varchar(45) DEFAULT NULL,
  `homepage` varchar(45) DEFAULT NULL,
  `url` varchar(45) DEFAULT NULL,
  `sourcetype` varchar(45) DEFAULT NULL,
  `sourcemd5` varchar(45) DEFAULT NULL,
  `sourcesha1` varchar(45) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`),
  KEY `sourcemd5_idx` (`sourcemd5`,`id`) USING BTREE,
  KEY `sourcesha1_idx` (`sourcesha1`,`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping events for database 'gaseous'
--

--
-- Dumping routines for database 'gaseous'
--

--
-- Final view structure for view `view_signatures_games`
--

DROP VIEW IF EXISTS `view_signatures_games`;
CREATE VIEW `view_signatures_games` AS
    SELECT 
        `signatures_games`.`id` AS `id`,
        `signatures_games`.`name` AS `name`,
        `signatures_games`.`description` AS `description`,
        `signatures_games`.`year` AS `year`,
        `signatures_games`.`publisherid` AS `publisherid`,
        `signatures_publishers`.`publisher` AS `publisher`,
        `signatures_games`.`demo` AS `demo`,
        `signatures_games`.`systemid` AS `platformid`,
        `signatures_platforms`.`platform` AS `platform`,
        `signatures_games`.`systemvariant` AS `systemvariant`,
        `signatures_games`.`video` AS `video`,
        `signatures_games`.`country` AS `country`,
        `signatures_games`.`language` AS `language`,
        `signatures_games`.`copyright` AS `copyright`
    FROM
        ((`signatures_games`
        JOIN `signatures_publishers` ON ((`signatures_games`.`publisherid` = `signatures_publishers`.`id`)))
        JOIN `signatures_platforms` ON ((`signatures_games`.`systemid` = `signatures_platforms`.`id`)));