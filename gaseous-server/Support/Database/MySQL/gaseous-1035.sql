-- Performance indexes for GetGames query
ALTER TABLE UserTimeTracking
ADD KEY idx_GameId_PlatformId_UserId (GameId, PlatformId, UserId);

ALTER TABLE RomMediaGroup
ADD KEY idx_GameId_PlatformId (GameId, PlatformId);

ALTER TABLE Games_Roms ADD KEY idx_MetadataMapId (MetadataMapId);