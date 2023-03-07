CREATE 
    ALGORITHM = UNDEFINED 
    DEFINER = `root`@`localhost` 
    SQL SECURITY DEFINER
VIEW `view_signatures_games` AS
    SELECT 
        `signatures_games`.`id` AS `id`,
        `signatures_games`.`name` AS `name`,
        `signatures_games`.`description` AS `description`,
        `signatures_games`.`year` AS `year`,
        `signatures_games`.`publisherid` AS `publisherid`,
        `signatures_publishers`.`publisher` AS `publisher`,
        `signatures_games`.`demo` AS `demo`,
        `signatures_games`.`systemid` AS `platformid`,
        `signatures_platforms`.`platform` AS `platform`,
        `signatures_games`.`systemvariant` AS `systemvariant`,
        `signatures_games`.`video` AS `video`,
        `signatures_games`.`country` AS `country`,
        `signatures_games`.`language` AS `language`,
        `signatures_games`.`copyright` AS `copyright`
    FROM
        ((`signatures_games`
        JOIN `signatures_publishers` ON ((`signatures_games`.`publisherid` = `signatures_publishers`.`id`)))
        JOIN `signatures_platforms` ON ((`signatures_games`.`systemid` = `signatures_platforms`.`id`)));

