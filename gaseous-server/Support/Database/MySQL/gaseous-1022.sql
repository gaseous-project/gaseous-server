CREATE TABLE `Signatures_RomToSource` (
  `SourceId` int NOT NULL,
  `RomId` int NOT NULL,
  PRIMARY KEY (`SourceId`, `RomId`)
);

CREATE TABLE `Signatures_Games_Countries` (
  `GameId` INT NOT NULL,
  `CountryId` INT NOT NULL,
  PRIMARY KEY (`GameId`, `CountryId`),
  CONSTRAINT `GameCountry` FOREIGN KEY (`GameId`) REFERENCES `Signatures_Games` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
);

CREATE TABLE `Signatures_Games_Languages` (
  `GameId` INT NOT NULL,
  `LanguageId` INT NOT NULL,
  PRIMARY KEY (`GameId`, `LanguageId`),
  CONSTRAINT `GameLanguage` FOREIGN KEY (`GameId`) REFERENCES `Signatures_Games` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
);

CREATE TABLE `Country` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Code` VARCHAR(20) NULL,
  `Value` VARCHAR(255) NULL,
  PRIMARY KEY (`Id`),
  INDEX `id_Code` (`Code` ASC) VISIBLE,
  INDEX `id_Value` (`Value` ASC) VISIBLE
);

CREATE TABLE `Language` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Code` VARCHAR(20) NULL,
  `Value` VARCHAR(255) NULL,
  PRIMARY KEY (`Id`),
  INDEX `id_Code` (`Code` ASC) VISIBLE,
  INDEX `id_Value` (`Value` ASC) VISIBLE
);