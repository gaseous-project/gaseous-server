CREATE TABLE `UserAvatars` (
  `UserId` VARCHAR(128) NOT NULL,
  `Id` VARCHAR(45) NOT NULL,
  `Avatar` LONGBLOB NULL,
  PRIMARY KEY (`UserId`),
  INDEX `idx_AvatarId` (`Id` ASC) VISIBLE,
  CONSTRAINT `ApplicationUser_Avatar`
    FOREIGN KEY (`UserId`)
    REFERENCES `Users` (`Id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION);
