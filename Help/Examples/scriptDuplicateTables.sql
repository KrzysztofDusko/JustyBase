#begin task_name C:\sqls\log.html
    #usingStart -- declare
    #usingEnd
    #goStart -- main
    #forBox(DB2) : for1
    {
        for $row in [
            select 
                NAME
            from 
                sysibm.systables
            where 
                CREATOR = 'TEST'
                and type = 'T'
            --SELECT 'EMPLOYEE' FROM SYSIBM.SYSDUMMY1
            --UNION ALL SELECT 'CUSTOMER' FROM SYSIBM.SYSDUMMY1
            --UNION ALL SELECT 'INVENTORY' FROM SYSIBM.SYSDUMMY1
            ]
        #importAdvanced(DB2) : $row[0] -- DB2 = name of connection
        {
            #type DB2 -- DB type
            --#connString Server=192.168.0.164:50000;Database=SAMPLE;UID=db2admin;PWD=password;
            #ConnectionString(DB2) -- if connection is definedm, if not -> #connString
            #importType reader -- reader/simple
            #exits false -- table exists
            #notifyAfter 10000
            #destination db2admin.$row[0]_X2 --destiionation schema.table
            #sqlStart
                SELECT * FROM TEST.$row[0]
            #sqlEnd
        }
    }
    #goEnd
#end


select 
    'DROP TABLE '  ||'db2admin.' || NAME ||';'
from 
    sysibm.systables
    where 
    CREATOR = 'DB2ADMIN'
    and type = 'T'
    AND NAME LIKE '%X2';