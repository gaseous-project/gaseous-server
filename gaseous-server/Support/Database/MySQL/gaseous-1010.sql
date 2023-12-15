CREATE TABLE `Statistics_Filters` (
  `FilterType` VARCHAR(25) NOT NULL,
  `TypeId` BIGINT NOT NULL,
  `Name` VARCHAR(45) NULL,
  `MaximumAgeRestriction` INT NULL,
  `IncludeUnrated` INT NULL,
  `GameCount` INT NULL,
  PRIMARY KEY (`FilterType`, `TypeId`, `MaximumAgeRestriction`, `IncludeUnrated`));
