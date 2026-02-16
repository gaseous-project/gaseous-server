CREATE TABLE `GameState` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT,
  `UserId` VARCHAR(45) NULL,
  `RomId` BIGINT NULL,
  `IsMediaGroup` INT NULL,
  `StateDateTime` DATETIME NULL,
  `Name` VARCHAR(100) NULL,
  `Screenshot` LONGBLOB NULL,
  `State` LONGBLOB NULL,
  PRIMARY KEY (`Id`),
  INDEX `idx_UserId` (`UserId` ASC) VISIBLE);
