#begin task_name C:\sqls\log.html
    #usingStart -- declare
    #usingEnd
    #goStart -- main
    #forBox(DB2) : for1
    {
        for $row in [
            SELECT 'EMPLOYEE' FROM SYSIBM.SYSDUMMY1
            UNION ALL SELECT 'CUSTOMER' FROM SYSIBM.SYSDUMMY1
            UNION ALL SELECT 'INVENTORY' FROM SYSIBM.SYSDUMMY1
            ]
        #sql(DB2) 
        {
            #sqlStart
                TRUNCATE TABLE DB2ADMIN.$row[0]_X1 IMMEDIATE;
            #sqlEnd
        }
        #importAdvanced(DB2) : $row[0]  -- DB2 = name of connection
        {
            #type DB2 -- DB type
            --#connString Server=192.168.0.164:50000;Database=SAMPLE;UID=db2admin;PWD=password;
            #ConnectionString(DB2) -- if connection is definedm, if not -> #connString
            #importType reader -- reader/simple
            #exits true -- table exists
            #notifyAfter 2
            #destination DB2ADMIN.$row[0]_X1 --destiionation schema.table
            #sqlStart
                SELECT * FROM TEST.$row[0]
            #sqlEnd
        }
    }
    #goEnd
#end


SELECT * FROM DB2ADMIN.EMPLOYEE_X1;
SELECT * FROM DB2ADMIN.CUSTOMER_X1;
SELECT * FROM DB2ADMIN.INVENTORY_X1;

TRUNCATE TABLE DB2ADMIN.EMPLOYEE_X1 IMMEDIATE;
TRUNCATE TABLE DB2ADMIN.CUSTOMER_X1 IMMEDIATE;
TRUNCATE TABLE DB2ADMIN.INVENTORY_X1 IMMEDIATE;
