CREATE INDEX idx_MetadataMap_Id_PlatformId ON MetadataMap (Id, PlatformId);

CREATE INDEX idx_GameSaves_RomId_IsMediaGroup_UserId ON GameSaves (RomId, IsMediaGroup, UserId);

CREATE INDEX idx_AgeGroup_GameId_SourceId_AgeGroupId ON AgeGroup (GameId, SourceId, AgeGroupId);

CREATE INDEX idx_GameLocalization_Game_SourceId_Region ON GameLocalization (Game, SourceId, Region);

CREATE INDEX idx_GameLocalization_Name ON GameLocalization (Name);

CREATE INDEX idx_Region_Id_SourceId_Identifier ON Region (Id, SourceId, Identifier);