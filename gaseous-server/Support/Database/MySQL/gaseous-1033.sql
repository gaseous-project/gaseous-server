CREATE TABLE `MetadataMap_Attachments` (
    `MetadataMapID` BIGINT NOT NULL,
    `AttachmentID` BIGINT NOT NULL,
    `AttachmentType` INT NOT NULL,
    `DateCreated` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UserId` VARCHAR(255) NULL,
    `SHA1` CHAR(40) NULL,
    `Filename` VARCHAR(255) NULL,
    `Size` BIGINT NULL,
    PRIMARY KEY (
        `MetadataMapID`,
        `AttachmentID`
    ),
    KEY `AttachmentID` (`AttachmentID`),
    CONSTRAINT `MetadataMap_Attachments_ibfk_1` FOREIGN KEY (`MetadataMapID`) REFERENCES `MetadataMap` (`MetadataMapID`) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `MetadataMap_Attachments_ibfk_2` FOREIGN KEY (`AttachmentID`) REFERENCES `Attachments` (`AttachmentID`) ON DELETE CASCADE ON UPDATE CASCADE
)