-- Reference data: Sport Types
-- Migrated from legacy COGDB Sport Type lookup table

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM SportTypes WHERE Code = 'FB')
    INSERT INTO SportTypes (Name, Code, IsActive, DisplayOrder) VALUES ('Football', 'FB', 1, 1);

IF NOT EXISTS (SELECT 1 FROM SportTypes WHERE Code = 'BB')
    INSERT INTO SportTypes (Name, Code, IsActive, DisplayOrder) VALUES ('Basketball', 'BB', 1, 2);

IF NOT EXISTS (SELECT 1 FROM SportTypes WHERE Code = 'BL')
    INSERT INTO SportTypes (Name, Code, IsActive, DisplayOrder) VALUES ('Baseball', 'BL', 1, 3);

IF NOT EXISTS (SELECT 1 FROM SportTypes WHERE Code = 'HK')
    INSERT INTO SportTypes (Name, Code, IsActive, DisplayOrder) VALUES ('Hockey', 'HK', 1, 4);

IF NOT EXISTS (SELECT 1 FROM SportTypes WHERE Code = 'SC')
    INSERT INTO SportTypes (Name, Code, IsActive, DisplayOrder) VALUES ('Soccer', 'SC', 1, 5);

IF NOT EXISTS (SELECT 1 FROM SportTypes WHERE Code = 'TN')
    INSERT INTO SportTypes (Name, Code, IsActive, DisplayOrder) VALUES ('Tennis', 'TN', 1, 6);

IF NOT EXISTS (SELECT 1 FROM SportTypes WHERE Code = 'MMA')
    INSERT INTO SportTypes (Name, Code, IsActive, DisplayOrder) VALUES ('MMA / Boxing', 'MMA', 1, 7);

IF NOT EXISTS (SELECT 1 FROM SportTypes WHERE Code = 'OTH')
    INSERT INTO SportTypes (Name, Code, IsActive, DisplayOrder) VALUES ('Other', 'OTH', 1, 99);
