
CREATE TABLE `platforms` (
  `id` bigint unsigned NOT NULL,
  `abbreviation` varchar(45) DEFAULT NULL,
  `alternative_name` varchar(45) DEFAULT NULL,
  `category` int DEFAULT NULL,
  `checksum` varchar(45) DEFAULT NULL,
  `created_at` datetime DEFAULT NULL,
  `generation` int DEFAULT NULL,
  `name` varchar(45) DEFAULT NULL,
  `platform_family` int DEFAULT NULL,
  `platform_logo` int DEFAULT NULL,
  `slug` varchar(45) DEFAULT NULL,
  `summary` varchar(255) DEFAULT NULL,
  `updated_at` datetime DEFAULT NULL,
  `url` varchar(255) DEFAULT NULL,
  `versions` json DEFAULT NULL,
  `websites` json DEFAULT NULL,
  `dateAdded` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
SELECT * FROM gaseous.platforms;