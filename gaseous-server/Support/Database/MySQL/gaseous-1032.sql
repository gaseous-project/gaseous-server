-- Add tables for authenticator keys and two-factor recovery codes
CREATE TABLE IF NOT EXISTS `UserAuthenticatorKeys` (
    `UserId` varchar(128) NOT NULL,
    `AuthenticatorKey` varchar(256) NOT NULL,
    PRIMARY KEY (`UserId`),
    CONSTRAINT `FK_UserAuthenticatorKeys_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
);

CREATE TABLE IF NOT EXISTS `UserRecoveryCodes` (
    `UserId` varchar(128) NOT NULL,
    `CodeHash` varchar(128) NOT NULL,
    PRIMARY KEY (`UserId`, `CodeHash`),
    CONSTRAINT `FK_UserRecoveryCodes_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
);