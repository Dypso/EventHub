-- Connect to PDB first
ALTER SESSION SET CONTAINER = TAPDB;

-- Create directory if not exists
CREATE OR REPLACE DIRECTORY DATA_DIR AS '/opt/oracle/oradata/TAPDB';

-- Create tablespace for tap data
CREATE TABLESPACE tap_data
DATAFILE '/opt/oracle/oradata/FREE/TAPDB/tap_data01.dbf' SIZE 500M
AUTOEXTEND ON NEXT 100M MAXSIZE 2G
EXTENT MANAGEMENT LOCAL
SEGMENT SPACE MANAGEMENT AUTO
NOLOGGING;

-- Create tap user with all necessary privileges
CREATE USER tapuser IDENTIFIED BY "TapUser123!"
DEFAULT TABLESPACE tap_data
QUOTA UNLIMITED ON tap_data;

-- Grant necessary privileges
GRANT CONNECT, RESOURCE TO tapuser;
GRANT CREATE SESSION TO tapuser;
GRANT AQ_ADMINISTRATOR_ROLE TO tapuser;
GRANT EXECUTE ON DBMS_AQ TO tapuser;
GRANT EXECUTE ON DBMS_AQADM TO tapuser;
GRANT SELECT ON DBA_QUEUE_TABLES TO tapuser;
GRANT SELECT ON DBA_QUEUES TO tapuser;
GRANT ANALYZE ANY TO tapuser;

-- Create objects as SYS to avoid permission issues
BEGIN
    DBMS_AQADM.CREATE_QUEUE_TABLE(
        queue_table => 'TAPUSER.TAP_QUEUE_TABLE',
        queue_payload_type => 'RAW',
        sort_list => 'ENQ_TIME',
        multiple_consumers => FALSE,
        storage_clause => 'TABLESPACE tap_data
                          PCTFREE 0
                          PCTUSED 99
                          INITRANS 20'
    );

    -- Create queue
    DBMS_AQADM.CREATE_QUEUE(
        queue_name => 'TAPUSER.TAP_QUEUE',
        queue_table => 'TAPUSER.TAP_QUEUE_TABLE',
        max_retries => 0,
        retry_delay => 0,
        retention_time => 0,
        comment => 'Queue for tap events'
    );

    -- Start queue
    DBMS_AQADM.START_QUEUE(
        queue_name => 'TAPUSER.TAP_QUEUE',
        enqueue => TRUE,
        dequeue => TRUE
    );
END;
/

-- Grant queue privileges
BEGIN
    DBMS_AQADM.GRANT_QUEUE_PRIVILEGE(
        privilege => 'ALL',
        queue_name => 'TAPUSER.TAP_QUEUE',
        grantee => 'TAPUSER'
    );
END;
/

-- Create index as SYS
CREATE INDEX tapuser.idx_tap_qt_msgid ON tapuser.TAP_QUEUE_TABLE(msgid)
TABLESPACE tap_data
NOLOGGING;

-- Optimize statistics
BEGIN
    DBMS_STATS.GATHER_TABLE_STATS(
        ownname => 'TAPUSER',
        tabname => 'TAP_QUEUE_TABLE',
        estimate_percent => 100,
        method_opt => 'FOR ALL COLUMNS SIZE AUTO',
        cascade => TRUE
    );
END;
/

-- Set session parameters for optimal performance
ALTER SESSION SET OPTIMIZER_INDEX_COST_ADJ = 1;
ALTER SESSION SET OPTIMIZER_INDEX_CACHING = 90;

-- Configure memory parameters conservatively for container
ALTER SYSTEM SET sga_target = 512M SCOPE=MEMORY;
ALTER SYSTEM SET pga_aggregate_target = 256M SCOPE=MEMORY;

-- Grant additional privileges for monitoring
GRANT SELECT ANY DICTIONARY TO tapuser;
GRANT SELECT ANY TABLE TO tapuser;

-- Verify setup
SELECT owner, queue_table FROM dba_queue_tables WHERE owner = 'TAPUSER';
SELECT owner, name, queue_type FROM dba_queues WHERE owner = 'TAPUSER';