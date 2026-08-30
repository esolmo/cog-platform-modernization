-- =============================================================================
-- Seed: Lottery Games and Sample Drawings
-- Must run AFTER lottery-service schema migration
-- Drawings are generated for the next 7 days — re-run daily via scheduled job
-- =============================================================================
SET NOCOUNT ON;

-- ─── Lottery Games ────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM LotteryGames WHERE GameType = 1)
    INSERT INTO LotteryGames (GameType, Name, IsActive)
    VALUES (1, 'Pick 3', 1);

IF NOT EXISTS (SELECT 1 FROM LotteryGames WHERE GameType = 2)
    INSERT INTO LotteryGames (GameType, Name, IsActive)
    VALUES (2, 'Pick 4', 1);

-- ─── Sample Drawings (next 7 days) ───────────────────────────────────────────
-- Production: replace this block with a scheduled SP that generates
-- drawings for the upcoming week based on the draw schedule.

DECLARE @Pick3Id INT = (SELECT Id FROM LotteryGames WHERE GameType = 1);
DECLARE @Pick4Id INT = (SELECT Id FROM LotteryGames WHERE GameType = 2);
DECLARE @Day INT = 0;

WHILE @Day < 7
BEGIN
    DECLARE @DrawDate DATE = DATEADD(DAY, @Day, CAST(GETUTCDATE() AS DATE));

    -- Morning draw — 11:00 UTC
    IF NOT EXISTS (
        SELECT 1 FROM DrawingDetails
        WHERE LotteryGameId = @Pick3Id
          AND CAST(DrawingDate AS DATE) = @DrawDate
          AND Name = 'Morning Draw'
    )
        INSERT INTO DrawingDetails (LotteryGameId, Name, DrawingDate, TimeZoneId, MinutesToDraw, IsActive)
        VALUES (@Pick3Id, 'Morning Draw',
                DATEADD(DAY, DATEDIFF(DAY, 0, @DrawDate), CAST('11:00:00' AS DATETIME2)),
                'America/New_York', 5, 1);

    -- Afternoon draw — 16:30 UTC
    IF NOT EXISTS (
        SELECT 1 FROM DrawingDetails
        WHERE LotteryGameId = @Pick3Id
          AND CAST(DrawingDate AS DATE) = @DrawDate
          AND Name = 'Afternoon Draw'
    )
        INSERT INTO DrawingDetails (LotteryGameId, Name, DrawingDate, TimeZoneId, MinutesToDraw, IsActive)
        VALUES (@Pick3Id, 'Afternoon Draw',
                DATEADD(DAY, DATEDIFF(DAY, 0, @DrawDate), CAST('16:30:00' AS DATETIME2)),
                'America/New_York', 5, 1);

    -- Evening draw — 22:00 UTC
    IF NOT EXISTS (
        SELECT 1 FROM DrawingDetails
        WHERE LotteryGameId = @Pick3Id
          AND CAST(DrawingDate AS DATE) = @DrawDate
          AND Name = 'Evening Draw'
    )
        INSERT INTO DrawingDetails (LotteryGameId, Name, DrawingDate, TimeZoneId, MinutesToDraw, IsActive)
        VALUES (@Pick3Id, 'Evening Draw',
                DATEADD(DAY, DATEDIFF(DAY, 0, @DrawDate), CAST('22:00:00' AS DATETIME2)),
                'America/New_York', 5, 1);

    -- Pick 4 draws same schedule
    IF NOT EXISTS (
        SELECT 1 FROM DrawingDetails
        WHERE LotteryGameId = @Pick4Id
          AND CAST(DrawingDate AS DATE) = @DrawDate
          AND Name = 'Midday Draw'
    )
        INSERT INTO DrawingDetails (LotteryGameId, Name, DrawingDate, TimeZoneId, MinutesToDraw, IsActive)
        VALUES (@Pick4Id, 'Midday Draw',
                DATEADD(DAY, DATEDIFF(DAY, 0, @DrawDate), CAST('13:00:00' AS DATETIME2)),
                'America/New_York', 5, 1);

    IF NOT EXISTS (
        SELECT 1 FROM DrawingDetails
        WHERE LotteryGameId = @Pick4Id
          AND CAST(DrawingDate AS DATE) = @DrawDate
          AND Name = 'Evening Draw'
    )
        INSERT INTO DrawingDetails (LotteryGameId, Name, DrawingDate, TimeZoneId, MinutesToDraw, IsActive)
        VALUES (@Pick4Id, 'Evening Draw',
                DATEADD(DAY, DATEDIFF(DAY, 0, @DrawDate), CAST('22:00:00' AS DATETIME2)),
                'America/New_York', 5, 1);

    SET @Day = @Day + 1;
END

PRINT 'Lottery games and drawings seeded.';
