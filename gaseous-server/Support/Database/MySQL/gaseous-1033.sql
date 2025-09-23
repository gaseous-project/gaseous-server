CREATE TABLE `MetadataMap_Attachments` (
    `MetadataMapID` BIGINT NOT NULL,
    `AttachmentID` bigint(20) NOT NULL AUTO_INCREMENT,
    `AttachmentType` INT NOT NULL,
    `DateCreated` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UserId` VARCHAR(255) NULL,
    `SHA1` CHAR(40) NULL,
    `Filename` VARCHAR(255) NULL,
    `FileSystemFilename` VARCHAR(255) NULL,
    `Size` BIGINT NULL,
    `IsShared` BOOLEAN NOT NULL DEFAULT FALSE,
    PRIMARY KEY (
        `MetadataMapID`,
        `AttachmentID`
    ),
    KEY `AttachmentID` (`AttachmentID`),
    CONSTRAINT `MetadataMap_Attachments_ibfk_1` FOREIGN KEY (`MetadataMapID`) REFERENCES `MetadataMap` (`Id`) ON DELETE CASCADE
);