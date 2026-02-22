ALTER TABLE `Games_Roms` 
ADD INDEX `id_IdAndLibraryId` (`Id` ASC, `LibraryId` ASC) VISIBLE;

ALTER TABLE `ServerLogs` 
ADD INDEX `idx_EventDate` (`EventTime` ASC) VISIBLE;