-- =============================================================================
-- Seed: System Configuration Defaults
-- These are the initial platform settings migrated from legacy Constants.asp
-- and server configuration. Encrypted values must be replaced before go-live.
-- =============================================================================
SET NOCOUNT ON;

DECLARE @SystemUserId INT = (SELECT TOP 1 Id FROM ApplicationUsers WHERE Username = 'system' ORDER BY Id);

-- Helper macro: upsert a config key
-- (SQL Server lacks native UPSERT for this pattern, using MERGE)

MERGE SystemConfigurations AS target
USING (VALUES
    -- General
    ('platform.name',               'COG Platform',          'General',  'Platform display name',                        0),
    ('platform.version',            '2.0.0',                 'General',  'Current platform version',                     0),
    ('platform.timezone',           'America/New_York',      'General',  'Default display timezone',                     0),
    ('platform.currency',           'USD',                   'General',  'Default currency code',                        0),

    -- Wager limits (from legacy system defaults)
    ('wager.min_straight',          '0.50',                  'Wagers',   'Minimum straight wager amount',                0),
    ('wager.max_straight',          '5000.00',               'Wagers',   'Maximum straight wager amount',                0),
    ('wager.min_parlay',            '1.00',                  'Wagers',   'Minimum parlay wager amount',                  0),
    ('wager.max_parlay',            '2000.00',               'Wagers',   'Maximum parlay wager amount',                  0),
    ('wager.max_payout',            '100000.00',             'Wagers',   'Maximum single-ticket payout',                 0),

    -- Lottery defaults
    ('lottery.pick3.prize_multiplier', '100',               'Lottery',  'Pick 3 prize multiplier per dollar wagered',   0),
    ('lottery.pick4.prize_multiplier', '1000',              'Lottery',  'Pick 4 prize multiplier per dollar wagered',   0),
    ('lottery.min_wager',           '0.50',                  'Lottery',  'Minimum lottery wager amount',                 0),

    -- Email (SMTP)
    ('smtp.host',                   'smtp.example.com',      'Email',    'SMTP server hostname',                         0),
    ('smtp.port',                   '587',                   'Email',    'SMTP server port',                             0),
    ('smtp.username',               'noreply@cog.local',     'Email',    'SMTP authentication username',                 0),
    ('smtp.password',               '',                      'Email',   'SMTP authentication password — set via AWS Secrets Manager (alerts-service: Email__Password env var)', 1),
    ('smtp.from_address',           'noreply@cog.local',     'Email',    'From address for system emails',               0),
    ('smtp.from_name',              'COG Platform',          'Email',    'Display name for system emails',               0),

    -- Security
    ('auth.access_token_minutes',   '15',                    'Security', 'JWT access token lifetime in minutes',         0),
    ('auth.refresh_token_days',     '30',                    'Security', 'Refresh token lifetime in days',               0),
    ('auth.max_failed_logins',      '5',                     'Security', 'Lockout after N failed login attempts',        0),
    ('auth.lockout_minutes',        '15',                    'Security', 'Account lockout duration in minutes',          0),

    -- Alerts
    ('alerts.vip_threshold',        '1000.00',               'Alerts',   'Default VIP alert minimum wager amount',       0),
    ('alerts.expiry_hours',         '4',                     'Alerts',   'Hours before an unacted alert expires',        0)

) AS source (ConfigKey, ConfigValue, Category, Description, IsEncrypted)
ON target.[Key] = source.ConfigKey
WHEN NOT MATCHED THEN
    INSERT ([Key], [Value], Category, Description, IsEncrypted, UpdatedByUserId)
    VALUES (source.ConfigKey, source.ConfigValue, source.Category, source.Description, source.IsEncrypted, @SystemUserId);

PRINT 'System configuration defaults seeded.';
