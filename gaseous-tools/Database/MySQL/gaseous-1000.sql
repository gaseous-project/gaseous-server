-- MySQL dump 10.13  Distrib 8.0.32, for macos13.0 (arm64)
--
-- Host: localhost    Database: gaseous
-- ------------------------------------------------------
-- Server version	8.0.32

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

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
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2023-02-27  8:54:22
