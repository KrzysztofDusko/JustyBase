#begin C:\sqls\log.html
    #usingStart
    #usingEnd
    #goStart -- main
    #sql(NPS_11.2.1.0.BETA) : drop
        {
            DROP TABLE demo2 IF EXISTS;
            DROP TABLE demo3 IF EXISTS;
            DROP TABLE demo4 IF EXISTS;
            DROP TABLE newest IF EXISTS;
            DROP TABLE demoA IF EXISTS;
            DROP TABLE demoB IF EXISTS;
            DROP TABLE demoC IF EXISTS;
        } 
        #importAdvanced(NPS_11.2.1.0.BETA) : import1
        {
            #type xlsx
            #connString  C:\sqls\DEMO_X.xlsx
            #tabs Arkusz2;Arkusz3;Arkusz1
            #destination demoA;demoB;demoC
        } 
        #importAdvanced(NPS_11.2.1.0.BETA) : import2
        {
            #type xlsx
            #connString  C:\sqls\DEMO2.xlsx
            #tabs aa a
            #destination demo2
        } 
        #importAdvanced(NPS_11.2.1.0.BETA) : import3 -- merged
        {
            #type xlsx
            #connString  C:\sqls\DEMO3.xlsx
            #tabs Arkusz1
            #destination demo3
        } 
        #importAdvanced(NPS_11.2.1.0.BETA) : import4 -- skipRows
        {
            #type xlsx
            #skipRows 4
            #connString  C:\sqls\DEMO4.xlsx
            #tabs Arkusz1
            #destination demo4
        } 
        #importAdvanced(NPS_11.2.1.0.BETA) : import5 -- newest
        {
            #type xlsx
            #skipRows 0
            #connString  NewestFileMtching(C:\sqls,DEMO*.xlsx)
            #tabs tab1
            #destination newest
        }
   #goEnd
#end
-- 
--SELECT * FROM demoA;
--SELECT * FROM demoB;
--SELECT * FROM demoC;
--SELECT * FROM demo2;
--SELECT * FROM demo3;
--SELECT * FROM demo4;
--SELECT * FROM newest;
--DROP TABLE demo4



