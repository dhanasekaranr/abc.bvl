-- ============================================================================
-- Primary Database Setup Script
-- Database: XEPDB1
-- User: APP_USER
-- Description: Creates ScreenDefinition and ScreenPilot tables with sample data
-- ============================================================================

-- Note: For Oracle XE, XEPDB1 is the pluggable database
-- No need to switch containers when connected directly to XEPDB1

-- Create Admin schema if not exists (run as SYSDBA first):
-- CREATE USER APP_USER IDENTIFIED BY App_User_Pass@ss1;
-- GRANT CONNECT, RESOURCE TO APP_USER;
-- GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, UNLIMITED TABLESPACE TO APP_USER;

-- Connect as APP_USER
-- sqlplus APP_USER/App_User_Pass@ss1@localhost:1521/XEPDB1

-- ============================================================================
-- DDL: ScreenDefinition Table
-- ============================================================================
CREATE TABLE APP_USER.Admin_ScreenDefn
(
    ScreenDefnId        NUMBER(19)      NOT NULL,
    ScreenCode          VARCHAR2(50)    NOT NULL,
    ScreenName          VARCHAR2(200)   NOT NULL,
    ScreenDesc          VARCHAR2(500),
    Status              NUMBER(3)       DEFAULT 1 NOT NULL,
    ScreenUrl           VARCHAR2(500),
    ParentScreenId      NUMBER(19),
    DisplayOrder        NUMBER(10)      DEFAULT 0,
    IconClass           VARCHAR2(100),
    CreatedAt           TIMESTAMP(6)    DEFAULT SYSTIMESTAMP NOT NULL,
    CreatedBy           VARCHAR2(100)   NOT NULL,
    UpdatedAt           TIMESTAMP(6)    DEFAULT SYSTIMESTAMP NOT NULL,
    UpdatedBy           VARCHAR2(100)   NOT NULL,
    RowVersion          VARCHAR2(50)    DEFAULT SYS_GUID() NOT NULL,
    
    CONSTRAINT PK_Admin_ScreenDefn PRIMARY KEY (ScreenDefnId),
    CONSTRAINT UK_Admin_ScreenDefn_Code UNIQUE (ScreenCode),
    CONSTRAINT CK_Admin_ScreenDefn_Status CHECK (Status IN (0, 1, 2)),
    CONSTRAINT FK_Admin_ScreenDefn_Parent FOREIGN KEY (ParentScreenId) 
        REFERENCES APP_USER.Admin_ScreenDefn(ScreenDefnId) ON DELETE SET NULL
);

-- Create indexes for performance
CREATE INDEX IDX_Admin_ScreenDefn_Status ON APP_USER.Admin_ScreenDefn(Status);
CREATE INDEX IDX_Admin_ScreenDefn_Parent ON APP_USER.Admin_ScreenDefn(ParentScreenId);
CREATE INDEX IDX_Admin_ScreenDefn_Name ON APP_USER.Admin_ScreenDefn(ScreenName);

-- Create sequence for ScreenDefnId
CREATE SEQUENCE APP_USER.SEQ_Admin_ScreenDefn
    START WITH 1000
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- Add comments
COMMENT ON TABLE APP_USER.Admin_ScreenDefn IS 'Stores screen/menu definitions for the admin tool';
COMMENT ON COLUMN APP_USER.Admin_ScreenDefn.ScreenDefnId IS 'Primary key - unique screen definition ID';
COMMENT ON COLUMN APP_USER.Admin_ScreenDefn.ScreenCode IS 'Unique screen code identifier';
COMMENT ON COLUMN APP_USER.Admin_ScreenDefn.ScreenName IS 'Display name of the screen';
COMMENT ON COLUMN APP_USER.Admin_ScreenDefn.Status IS 'Status: 0=Inactive, 1=Active, 2=Pending';
COMMENT ON COLUMN APP_USER.Admin_ScreenDefn.RowVersion IS 'Optimistic concurrency control version';

-- ============================================================================
-- DDL: ScreenPilot Table (User-to-Screen Assignment)
-- ============================================================================
CREATE TABLE APP_USER.Admin_ScreenPilot
(
    ScreenPilotId       NUMBER(19)      NOT NULL,
    ScreenDefnId        NUMBER(19)      NOT NULL,
    UserId              VARCHAR2(100)   NOT NULL,
    Status              NUMBER(3)       DEFAULT 1 NOT NULL,
    UpdatedAt           TIMESTAMP(6)    DEFAULT SYSTIMESTAMP NOT NULL,
    UpdatedBy           VARCHAR2(100)   NOT NULL,
    RowVersion          VARCHAR2(50)    DEFAULT SYS_GUID() NOT NULL,
    ScreenCode          VARCHAR2(50),
    ScreenName          VARCHAR2(200),
    
    CONSTRAINT PK_Admin_ScreenPilot PRIMARY KEY (ScreenPilotId),
    CONSTRAINT UK_Admin_ScreenPilot UNIQUE (ScreenDefnId, UserId),
    CONSTRAINT CK_Admin_ScreenPilot_Status CHECK (Status IN (0, 1)),
    CONSTRAINT FK_Admin_ScreenPilot_Screen FOREIGN KEY (ScreenDefnId) 
        REFERENCES APP_USER.Admin_ScreenDefn(ScreenDefnId) ON DELETE CASCADE
);

-- Create indexes for performance
CREATE INDEX IDX_Admin_ScreenPilot_Screen ON APP_USER.Admin_ScreenPilot(ScreenDefnId);
CREATE INDEX IDX_Admin_ScreenPilot_User ON APP_USER.Admin_ScreenPilot(UserId);
CREATE INDEX IDX_Admin_ScreenPilot_Status ON APP_USER.Admin_ScreenPilot(Status);

-- Create sequence for ScreenPilotId
CREATE SEQUENCE APP_USER.SEQ_Admin_ScreenPilot
    START WITH 1000
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- Add comments
COMMENT ON TABLE APP_USER.Admin_ScreenPilot IS 'User-to-screen assignment/access control';
COMMENT ON COLUMN APP_USER.Admin_ScreenPilot.ScreenPilotId IS 'Primary key - unique assignment ID';
COMMENT ON COLUMN APP_USER.Admin_ScreenPilot.UserId IS 'User identifier who has access to the screen';
COMMENT ON COLUMN APP_USER.Admin_ScreenPilot.Status IS 'Status: 0=Inactive, 1=Active';

-- ============================================================================
-- DML: Sample Data for ScreenDefinition
-- ============================================================================

-- Insert root/parent menu items
INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'DASH001', 'Dashboard', 'Main dashboard view', 1, '/dashboard', NULL, 1, 'fa-dashboard', 'SYSTEM', 'SYSTEM');

INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'ADM001', 'Administration', 'Administration menu', 1, NULL, NULL, 2, 'fa-cog', 'SYSTEM', 'SYSTEM');

INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'REP001', 'Reports', 'Reports menu', 1, NULL, NULL, 3, 'fa-bar-chart', 'SYSTEM', 'SYSTEM');

-- Insert child menu items under Administration
INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'ADM_USR001', 'User Management', 'Manage system users', 1, '/admin/users', 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'ADM001'), 1, 'fa-users', 'SYSTEM', 'SYSTEM');

INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'ADM_SCR001', 'Screen Management', 'Manage screen definitions', 1, '/admin/screens', 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'ADM001'), 2, 'fa-desktop', 'SYSTEM', 'SYSTEM');

INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'ADM_ROL001', 'Role Management', 'Manage user roles', 1, '/admin/roles', 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'ADM001'), 3, 'fa-shield', 'SYSTEM', 'SYSTEM');

INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'ADM_SET001', 'System Settings', 'Configure system settings', 1, '/admin/settings', 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'ADM001'), 4, 'fa-wrench', 'SYSTEM', 'SYSTEM');

-- Insert child menu items under Reports
INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'REP_USG001', 'Usage Report', 'System usage statistics', 1, '/reports/usage', 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'REP001'), 1, 'fa-line-chart', 'SYSTEM', 'SYSTEM');

INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'REP_AUD001', 'Audit Report', 'Audit trail report', 1, '/reports/audit', 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'REP001'), 2, 'fa-history', 'SYSTEM', 'SYSTEM');

-- Insert some inactive/pending screens for testing
INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'TEST001', 'Test Screen', 'Test screen (inactive)', 0, '/test', NULL, 99, 'fa-flask', 'SYSTEM', 'SYSTEM');

INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, ScreenUrl, ParentScreenId, DisplayOrder, IconClass, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'BETA001', 'Beta Feature', 'Beta feature (pending)', 2, '/beta', NULL, 98, 'fa-flag', 'SYSTEM', 'SYSTEM');

-- ============================================================================
-- DML: Sample Data for ScreenPilot (User Assignments)
-- ============================================================================

-- Assign dashboard to multiple users
INSERT INTO APP_USER.Admin_ScreenPilot 
(ScreenPilotId, ScreenDefnId, UserId, Status, UpdatedBy, ScreenCode, ScreenName)
VALUES 
(SEQ_Admin_ScreenPilot.NEXTVAL, 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'DASH001'),
    'john.doe', 1, 'SYSTEM', 'DASH001', 'Dashboard');

INSERT INTO APP_USER.Admin_ScreenPilot 
(ScreenPilotId, ScreenDefnId, UserId, Status, UpdatedBy, ScreenCode, ScreenName)
VALUES 
(SEQ_Admin_ScreenPilot.NEXTVAL, 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'DASH001'),
    'jane.smith', 1, 'SYSTEM', 'DASH001', 'Dashboard');

INSERT INTO APP_USER.Admin_ScreenPilot 
(ScreenPilotId, ScreenDefnId, UserId, Status, UpdatedBy, ScreenCode, ScreenName)
VALUES 
(SEQ_Admin_ScreenPilot.NEXTVAL, 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'DASH001'),
    'admin.user', 1, 'SYSTEM', 'DASH001', 'Dashboard');

-- Assign admin screens to admin users
INSERT INTO APP_USER.Admin_ScreenPilot 
(ScreenPilotId, ScreenDefnId, UserId, Status, UpdatedBy, ScreenCode, ScreenName)
VALUES 
(SEQ_Admin_ScreenPilot.NEXTVAL, 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'ADM_USR001'),
    'admin.user', 1, 'SYSTEM', 'ADM_USR001', 'User Management');

INSERT INTO APP_USER.Admin_ScreenPilot 
(ScreenPilotId, ScreenDefnId, UserId, Status, UpdatedBy, ScreenCode, ScreenName)
VALUES 
(SEQ_Admin_ScreenPilot.NEXTVAL, 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'ADM_SCR001'),
    'admin.user', 1, 'SYSTEM', 'ADM_SCR001', 'Screen Management');

INSERT INTO APP_USER.Admin_ScreenPilot 
(ScreenPilotId, ScreenDefnId, UserId, Status, UpdatedBy, ScreenCode, ScreenName)
VALUES 
(SEQ_Admin_ScreenPilot.NEXTVAL, 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'ADM_ROL001'),
    'admin.user', 1, 'SYSTEM', 'ADM_ROL001', 'Role Management');

-- Assign reports to users
INSERT INTO APP_USER.Admin_ScreenPilot 
(ScreenPilotId, ScreenDefnId, UserId, Status, UpdatedBy, ScreenCode, ScreenName)
VALUES 
(SEQ_Admin_ScreenPilot.NEXTVAL, 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'REP_USG001'),
    'john.doe', 1, 'SYSTEM', 'REP_USG001', 'Usage Report');

INSERT INTO APP_USER.Admin_ScreenPilot 
(ScreenPilotId, ScreenDefnId, UserId, Status, UpdatedBy, ScreenCode, ScreenName)
VALUES 
(SEQ_Admin_ScreenPilot.NEXTVAL, 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'REP_AUD001'),
    'admin.user', 1, 'SYSTEM', 'REP_AUD001', 'Audit Report');

-- Cross-user assignments
INSERT INTO APP_USER.Admin_ScreenPilot 
(ScreenPilotId, ScreenDefnId, UserId, Status, UpdatedBy, ScreenCode, ScreenName)
VALUES 
(SEQ_Admin_ScreenPilot.NEXTVAL, 
    (SELECT ScreenDefnId FROM APP_USER.Admin_ScreenDefn WHERE ScreenCode = 'ADM_SCR001'),
    'jane.smith', 1, 'SYSTEM', 'ADM_SCR001', 'Screen Management');

-- ============================================================================
-- Verification Queries
-- ============================================================================

-- Verify data
SELECT 'ScreenDefinition Count' AS Info, COUNT(*) AS Count FROM APP_USER.Admin_ScreenDefn
UNION ALL
SELECT 'ScreenPilot Count', COUNT(*) FROM APP_USER.Admin_ScreenPilot;

-- Show all screens with hierarchy
SELECT 
    s.ScreenDefnId,
    s.ScreenCode,
    s.ScreenName,
    s.Status,
    s.DisplayOrder,
    p.ScreenName AS ParentScreen
FROM APP_USER.Admin_ScreenDefn s
LEFT JOIN APP_USER.Admin_ScreenDefn p ON s.ParentScreenId = p.ScreenDefnId
ORDER BY NVL(s.ParentScreenId, 0), s.DisplayOrder;

-- Show user screen assignments
SELECT 
    sp.UserId,
    sd.ScreenCode,
    sd.ScreenName,
    sp.Status AS AssignmentStatus
FROM APP_USER.Admin_ScreenPilot sp
JOIN APP_USER.Admin_ScreenDefn sd ON sp.ScreenDefnId = sd.ScreenDefnId
ORDER BY sp.UserId, sd.ScreenCode;

COMMIT;

-- ============================================================================
-- Grant necessary permissions (run as SYSTEM or DBA)
-- ============================================================================
-- GRANT SELECT, INSERT, UPDATE, DELETE ON APP_USER.Admin_ScreenDefn TO <your_api_user>;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON APP_USER.Admin_ScreenPilot TO <your_api_user>;
-- GRANT SELECT ON APP_USER.SEQ_Admin_ScreenDefn TO <your_api_user>;
-- GRANT SELECT ON APP_USER.SEQ_Admin_ScreenPilot TO <your_api_user>;

-- ============================================================================
-- End of Primary Database Setup Script
-- ============================================================================
