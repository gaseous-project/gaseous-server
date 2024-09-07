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