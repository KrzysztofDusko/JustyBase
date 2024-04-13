#begin task_name C:\sqls\log.html
    #usingStart -- declare
    #usingEnd
    #goStart -- main
    #sql(DB2) 
    {
        #sqlStart
            DROP TABLE TEST.ACT_X2 IF EXISTS;
        #sqlEnd
    }
    #importAdvanced(DB2) -- DB2 = name of connection
    {
        #type DB2 -- DB type
        #connString Server=192.168.0.164:50000;Database=SAMPLE;UID=db2admin;PWD=password;
        --#ConnectionString(DB2) -- if connection is definedm, if not -> #connString
        #importType reader -- reader/simple
        #exits false -- table not exists
        #destination TEST.ACT_X2 --destiionation schema.table
        #sqlStart
            SELECT * FROM TEST.ACT
        #sqlEnd
    }

    #goEnd
#end

