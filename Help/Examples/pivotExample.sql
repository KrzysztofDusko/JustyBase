#beginMt task_name C:\sqls\log.html
    #usingStart 
    #usingEnd
    #goStart
        #exportToFileAdvanced(MsSqlLocal)
        {
            #type xlsx
            #path  C:\sqls\pivotTableExample.xlsx     
            #tabname source -- if provided ... 
            #pivotTableName pivotTable1 --, not required
            --#startCell A1 -- default A1, not required
            --#forceRefresh true -- true/false, default true, not required
            #sqlStart
                SELECT top 50 a.* FROM dbo.DimProductSubcategory a
            #sqlEnd
        }
    #goEnd
#end