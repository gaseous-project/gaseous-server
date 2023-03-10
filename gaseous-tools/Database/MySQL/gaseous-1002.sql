CREATE TABLE `gaseous`.`settings` (
  `setting` VARCHAR(45) NOT NULL,
  `value` LONGTEXT NULL,
  UNIQUE INDEX `setting_UNIQUE` (`setting` ASC) VISIBLE,
  PRIMARY KEY (`setting`));

