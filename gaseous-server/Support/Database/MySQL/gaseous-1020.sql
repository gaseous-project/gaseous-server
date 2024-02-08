CREATE TABLE `UserTimeTracking` (
  `GameId` BIGINT NULL,
  `UserId` VARCHAR(45) NULL,
  `SessionId` VARCHAR(45) NULL,
  `SessionTime` DATETIME NULL,
  `SessionLength` INT NULL,
  INDEX `UserId_idx` (`UserId` ASC) VISIBLE,
  INDEX `SessionId_idx` (`SessionId` ASC) VISIBLE,
  CONSTRAINT `UserId`
    FOREIGN KEY (`UserId`)
    REFERENCES `Users` (`Id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION);
