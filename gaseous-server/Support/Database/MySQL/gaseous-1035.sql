ALTER TABLE UserTimeTracking
ADD KEY idx_GameId_PlatformId_UserId (GameId, PlatformId, UserId);

ALTER TABLE RomMediaGroup
ADD KEY idx_GameId_PlatformId (GameId, PlatformId);

ALTER TABLE Games_Roms ADD KEY idx_MetadataMapId (MetadataMapId);

CREATE INDEX idx_Relation_GameModes_GameId_SourceId ON Relation_Game_GameModes (
    GameId,
    GameSourceId,
    GameModesId
);

CREATE INDEX idx_Relation_PlayerPerspectives_GameId_SourceId ON Relation_Game_PlayerPerspectives (
    GameId,
    GameSourceId,
    PlayerPerspectivesId
);

CREATE INDEX idx_Relation_Themes_GameId_SourceId ON Relation_Game_Themes (
    GameId,
    GameSourceId,
    ThemesId
);

CREATE INDEX IF NOT EXISTS idx_Relation_Genres_composite ON Relation_Game_Genres (
    GameId,
    GameSourceId,
    GenresId
);

CREATE INDEX idx_Games_Roms_GameId ON Games_Roms (GameId);

CREATE INDEX idx_Games_Roms_MetadataMapId ON Games_Roms (MetadataMapId);

CREATE INDEX idx_Metadata_Game_Id_SourceId ON Metadata_Game (Id, SourceId);

CREATE INDEX IF NOT EXISTS idx_Games_Roms_MetadataMapId_PlatformId ON Games_Roms (MetadataMapId, PlatformId);

CREATE INDEX IF NOT EXISTS idx_Metadata_Platform_Id_SourceId ON Metadata_Platform (Id, SourceId);