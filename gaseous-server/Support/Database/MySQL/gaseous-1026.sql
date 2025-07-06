CREATE OR REPLACE VIEW `view_Signatures_Games` AS
SELECT
    `Signatures_Games`.`Id` AS `Id`,
    `Signatures_Games`.`Name` AS `Name`,
    `Signatures_Games`.`Description` AS `Description`,
    `Signatures_Games`.`Year` AS `Year`,
    `Signatures_Games`.`PublisherId` AS `PublisherId`,
    `Signatures_Publishers`.`Publisher` AS `Publisher`,
    `Signatures_Games`.`Demo` AS `Demo`,
    `Signatures_Games`.`SystemId` AS `PlatformId`,
    `Signatures_Platforms`.`Platform` AS `Platform`,
    `Signatures_Games`.`SystemVariant` AS `SystemVariant`,
    `Signatures_Games`.`Video` AS `Video`,
    `Signatures_Games`.`Country` AS `Country`,
    `Signatures_Games`.`Language` AS `Language`,
    `Signatures_Games`.`Copyright` AS `Copyright`
FROM (
        (
            `Signatures_Games`
            LEFT JOIN `Signatures_Publishers` ON (
                `Signatures_Games`.`PublisherId` = `Signatures_Publishers`.`Id`
            )
        )
        JOIN `Signatures_Platforms` ON (
            `Signatures_Games`.`SystemId` = `Signatures_Platforms`.`Id`
        )
    );