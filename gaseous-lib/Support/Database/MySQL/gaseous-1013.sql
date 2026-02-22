CREATE TABLE `SearchCache` (
  `SearchFields` varchar(384) NOT NULL,
  `SearchString` varchar(128) NOT NULL,
  `Content` longtext DEFAULT NULL,
  `LastSearch` datetime DEFAULT NULL,
  PRIMARY KEY (`SearchFields`,`SearchString`),
  KEY `idx_SearchString` (`SearchFields`,`SearchString`),
  KEY `idx_LastSearch` (`LastSearch`)
);