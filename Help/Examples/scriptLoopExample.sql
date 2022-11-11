 #begin task_name C:\sqls\log.html
    #usingStart
        #text $scripthPath : 10
        #text $par1 : 11
    #usingEnd

    #goStart -- main
        #forBox(NPS_11.2.1.0.BETA) : paczka2
        {
            for $row in [SELECT D.DATEKEY, D.ENGLISHDAYNAMEOFWEEK FROM JUST_DATA..DIMDATE D WHERE D.DATEKEY BETWEEN 20050101 AND 20050131 ORDER BY D.DATEKEY LIMIT 3]
            #exportToFile(NPS_11.2.1.0.BETA,C:\sqls\DimCurrency$row[0].xlsx) : INNER1$row[0]
            {
                SELECT $row[0] FROM JUST_DATA..DimCurrency LIMIT 1
            }
            #exportToFile(NPS_11.2.1.0.BETA,C:\sqls\DimCurrency10$row[0].xlsx) : INNER110$row[0]
            {
                SELECT '$row[1]' FROM JUST_DATA..DimCurrency LIMIT 1
            }
        }
    #goEnd
#end