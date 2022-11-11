 #begin task_name C:\sqls\log.html
    #usingStart
        #text $scripthPath : 10
        #text $par1 : 11
        #text $par2 : 0
    #usingEnd
    #goStart -- main
        #exportToFile(MsSqlLocal,C:\sqls\DimCurrency0.csv) : START
        {
            SELECT 'START - VAL' AS START
        }
        #box() : box1
        {
            #exportToFile(MsSqlLocal,C:\sqls\DimCurrency1.csv) : INNER1
            {
                SELECT 'LEFT - VAL' AS LEFT1
            }
            #setFromRaw($scripthPath,1) : variableSet1
            {
            }
            #exportToFile(NPS_11.2.1.0.BETA,C:\sqls\FactProductInventory.xlsx) : INNER3
            {
                SELECT * FROM JUST_DATA..FactProductInventory  LIMIT 10000
            }
            #box() : box2
            {
                if ($scripthPath + 2 ) * ($scripthPath + 1) = $scripthPath + 5
                #run(C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe,https://github.com/KrzysztofDusko/Just-Data) : INNER11
                {
                }
                #run(C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe,https://github.com/KrzysztofDusko/Just-Data/issues) : INNER12
                {
                }
                #exportToFile(NPS_11.2.1.0.BETA,C:\sqls\DimCurrency.xlsx) : INNER13
                {
                    SELECT * FROM JUST_DATA..DimCurrency
                }
            }
        }
        #forBox(NPS_11.2.1.0.BETA) : box3
        {
            for $row in [SELECT D.DATEKEY, D.DAYNUMBEROFWEEK FROM JUST_DATA..DIMDATE D WHERE D.DATEKEY BETWEEN 20050101 AND 20050131 ORDER BY D.DATEKEY]
            #box() : box13
            {
                if ($row[0]+1) % 2 = 0
                #exportToFile(NPS_11.2.1.0.BETA,C:\sqls\DimCurrency$row[0].xlsx) : INNER1$row[0]
                {
                    SELECT $row[0] FROM JUST_DATA..DimCurrency LIMIT $row[1]
                }
            }
        }
        #setFromSql(NPS_11.2.1.0.BETA,$par2) : variableSet2
        {
            SELECT 17;
        }
        #exportToFile(MsSqlLocal,C:\sqls\DimCurrency2.csv) : POST
        {
            SELECT $par2,*,$scripthPath as xxx FROM dbo.DimCurrency 
        }
        #run(C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe,C:\sqls\log.html) : POST2
        {
        }
    #goEnd
#end