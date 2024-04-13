#begin task_name C:\sqls\log.html
    #usingStart -- declare
    #usingEnd
    #goStart -- main
    #exportToFileAdvanced(DB2)
        {
            #type text
            #path  C:\sqls\BIGTABLE.csv
            #delimiter |
            #lineDelimiter windows
            #nullValue 
            #header true
            #encoding UTF8
            #compression none
            #sqlStart
            SELECT * FROM DB2ADMIN.BIGTABLE 
            #sqlEnd
        }
    #goEnd
#end
