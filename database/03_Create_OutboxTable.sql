-- ============================================================================
-- Outbox Table Setup Script
-- Database: XEPDB1
-- User: APP_USER
-- Description: Creates AdminToolOutBox table for transactional outbox pattern
--              Used for eventual consistency between dual databases
-- ============================================================================

-- Note: For Oracle XE with single XEPDB1 instance
-- Connect directly to XEPDB1, no container switching needed

-- Connect as APP_USER:
-- sqlplus APP_USER/App_User_Pass@ss1@localhost:1521/XEPDB1

-- ============================================================================
-- DDL: AdminToolOutBox Table
-- ============================================================================
CREATE TABLE APP_USER.CVLWebTools_AdminToolOutBox
(
    OutBoxId            NUMBER(19)      NOT NULL,
    EntityType          VARCHAR2(100)   NOT NULL,
    EntityId            NUMBER(19)      NOT NULL,
    Operation           VARCHAR2(20)    NOT NULL,
    Payload             CLOB            NOT NULL,
    CreatedAt           TIMESTAMP(6)    DEFAULT SYSTIMESTAMP NOT NULL,
    ProcessedAt         TIMESTAMP(6),
    Status              VARCHAR2(20)    DEFAULT 'Pending' NOT NULL,
    RetryCount          NUMBER(10)      DEFAULT 0 NOT NULL,
    ErrorMessage        VARCHAR2(4000),
    SourceDatabase      VARCHAR2(50)    NOT NULL,
    TargetDatabase      VARCHAR2(50)    NOT NULL,
    CorrelationId       VARCHAR2(100),
    
    CONSTRAINT PK_CVLWebTools_AdminToolOutBox PRIMARY KEY (OutBoxId),
    CONSTRAINT CK_AdminToolOutBox_Operation CHECK (Operation IN ('INSERT', 'UPDATE', 'DELETE')),
    CONSTRAINT CK_AdminToolOutBox_Status CHECK (Status IN ('Pending', 'Processing', 'Completed', 'Failed'))
);

-- Create indexes for performance
CREATE INDEX IDX_AdminToolOutBox_Status ON APP_USER.CVLWebTools_AdminToolOutBox(Status, CreatedAt);
CREATE INDEX IDX_AdminToolOutBox_Entity ON APP_USER.CVLWebTools_AdminToolOutBox(EntityType, EntityId);
CREATE INDEX IDX_AdminToolOutBox_Created ON APP_USER.CVLWebTools_AdminToolOutBox(CreatedAt);
CREATE INDEX IDX_AdminToolOutBox_Correlation ON APP_USER.CVLWebTools_AdminToolOutBox(CorrelationId);

-- Create sequence for OutBoxId
CREATE SEQUENCE APP_USER.SEQ_CVLWebTools_AdminToolOutBox
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- Add comments
COMMENT ON TABLE APP_USER.CVLWebTools_AdminToolOutBox IS 'Transactional outbox for dual-database synchronization';
COMMENT ON COLUMN APP_USER.CVLWebTools_AdminToolOutBox.OutBoxId IS 'Primary key - unique outbox message ID';
COMMENT ON COLUMN APP_USER.CVLWebTools_AdminToolOutBox.EntityType IS 'Type of entity (ScreenDefinition, ScreenPilot, etc.)';
COMMENT ON COLUMN APP_USER.CVLWebTools_AdminToolOutBox.EntityId IS 'ID of the entity being synchronized';
COMMENT ON COLUMN APP_USER.CVLWebTools_AdminToolOutBox.Operation IS 'Type of operation: INSERT, UPDATE, DELETE';
COMMENT ON COLUMN APP_USER.CVLWebTools_AdminToolOutBox.Payload IS 'JSON payload of the entity data';
COMMENT ON COLUMN APP_USER.CVLWebTools_AdminToolOutBox.Status IS 'Processing status: Pending, Processing, Completed, Failed';
COMMENT ON COLUMN APP_USER.CVLWebTools_AdminToolOutBox.SourceDatabase IS 'Database where change originated';
COMMENT ON COLUMN APP_USER.CVLWebTools_AdminToolOutBox.TargetDatabase IS 'Database to synchronize to';
COMMENT ON COLUMN APP_USER.CVLWebTools_AdminToolOutBox.CorrelationId IS 'Request correlation ID for tracing';

-- ============================================================================
-- Sample Outbox Messages (for testing)
-- ============================================================================

-- Example: Screen creation in primary needs sync to secondary
INSERT INTO APP_USER.CVLWebTools_AdminToolOutBox
(OutBoxId, EntityType, EntityId, Operation, Payload, SourceDatabase, TargetDatabase, CorrelationId)
VALUES
(SEQ_CVLWebTools_AdminToolOutBox.NEXTVAL,
'ScreenDefinition',
1001,
'INSERT',
'{"screenDefnId":1001,"screenCode":"NEW001","screenName":"New Screen","status":1}',
'primarydb',
'secondarydb',
'CORR-001');

-- Example: Screen update in primary needs sync to secondary
INSERT INTO APP_USER.CVLWebTools_AdminToolOutBox
(OutBoxId, EntityType, EntityId, Operation, Payload, SourceDatabase, TargetDatabase, CorrelationId)
VALUES
(SEQ_CVLWebTools_AdminToolOutBox.NEXTVAL,
'ScreenDefinition',
1000,
'UPDATE',
'{"screenDefnId":1000,"screenCode":"DASH001","screenName":"Dashboard Updated","status":1}',
'primarydb',
'secondarydb',
'CORR-002');

-- Example: Failed message (for testing retry logic)
INSERT INTO APP_USER.CVLWebTools_AdminToolOutBox
(OutBoxId, EntityType, EntityId, Operation, Payload, Status, RetryCount, ErrorMessage, SourceDatabase, TargetDatabase, CorrelationId)
VALUES
(SEQ_CVLWebTools_AdminToolOutBox.NEXTVAL,
'ScreenPilot',
1005,
'DELETE',
'{"screenPilotId":1005}',
'Failed',
3,
'Connection timeout to target database',
'primarydb',
'secondarydb',
'CORR-003');

-- ============================================================================
-- Verification Queries
-- ============================================================================

-- Check outbox message counts by status
SELECT 
    Status,
    COUNT(*) AS MessageCount
FROM APP_USER.CVLWebTools_AdminToolOutBox
GROUP BY Status
ORDER BY Status;

-- View pending messages
SELECT 
    OutBoxId,
    EntityType,
    EntityId,
    Operation,
    CreatedAt,
    RetryCount,
    CorrelationId
FROM APP_USER.CVLWebTools_AdminToolOutBox
WHERE Status = 'Pending'
ORDER BY CreatedAt;

-- View failed messages
SELECT 
    OutBoxId,
    EntityType,
    EntityId,
    Operation,
    RetryCount,
    ErrorMessage,
    CreatedAt
FROM APP_USER.CVLWebTools_AdminToolOutBox
WHERE Status = 'Failed'
ORDER BY CreatedAt DESC;

COMMIT;

-- ============================================================================
-- Cleanup Query (for testing - removes old processed messages)
-- ============================================================================
/*
-- Delete completed messages older than 30 days
DELETE FROM APP_USER.CVLWebTools_AdminToolOutBox
WHERE Status = 'Completed'
  AND ProcessedAt < SYSTIMESTAMP - INTERVAL '30' DAY;

-- Reset failed messages for retry (use with caution)
UPDATE APP_USER.CVLWebTools_AdminToolOutBox
SET Status = 'Pending',
    RetryCount = 0,
    ErrorMessage = NULL
WHERE Status = 'Failed'
  AND RetryCount < 5;

COMMIT;
*/

-- ============================================================================
-- Grant necessary permissions (run as SYSTEM or DBA)
-- ============================================================================
-- GRANT SELECT, INSERT, UPDATE, DELETE ON APP_USER.CVLWebTools_AdminToolOutBox TO <your_api_user>;
-- GRANT SELECT ON APP_USER.SEQ_CVLWebTools_AdminToolOutBox TO <your_api_user>;

-- ============================================================================
-- End of Outbox Table Setup Script
-- ============================================================================
