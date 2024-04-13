#begin task_name C:\sqls\log.html
    #usingStart -- declare
    #usingEnd
    #goStart -- main
    #sql(DB2) 
    {
        #sqlStart
            DROP TABLE TEST.ACT_X2 IF EXISTS;
            CREATE TABLE TEST.ACT_X2
            (
                ACTNO SMALLINT NOT NULL,
                ACTKWD CHARACTER(6) NOT NULL,
                ACTDESC VARCHAR(20) NOT NULL,
                TEN INTEGER
            )
            ORGANIZE BY ROW IN USERSPACE1
            COMPRESS NO;
        #sqlEnd
    }
    #importAdvanced(DB2) -- DB2 = name of connection
    {
        #type DB2 -- DB type
        #connString Server=192.168.0.164:50000;Database=SAMPLE;UID=db2admin;PWD=password;
        --#ConnectionString(DB2) -- if connection is definedm, if not -> #connString
        #importType reader -- reader/simple
        #exits true -- table exists
        #destination TEST.ACT_X2 --destiionation schema.table
        #sqlStart
            SELECT *,10 AS TEN FROM TEST.ACT
        #sqlEnd
    }
    #goEnd
#end

