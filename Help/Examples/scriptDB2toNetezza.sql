#begin task_name C:\sqls\log.html
    #usingStart -- declare
    #usingEnd
    #goStart -- main

    #importAdvanced(NPS_11.2.1.0.BETA) -- DB2 = name of connection
    {
        #type DB2 -- DB type
        #ConnectionString(DB2) -- if connection is definedm, if not -> #connString
        #importType reader -- reader/simple
        --#exits false -- only potion for NZ
        #destination JUST_DATA..EMPLOYEE_IMPORTED --destiionation schema.table
        #sqlStart
            SELECT * FROM TEST.EMPLOYEE
        #sqlEnd
    }
    #goEnd
#end

SELECT * FROM JUST_DATA..EMPLOYEE_IMPORTED