CREATE TABLE `Favourites` (
  `UserId` varchar(45) NOT NULL,
  `GameId` bigint(20) NOT NULL,
  PRIMARY KEY (`UserId`,`GameId`),
  KEY `idx_GameId` (`GameId`),
  KEY `idx_UserId` (`UserId`),
  CONSTRAINT `ApplicationUser_Favourite` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
);
