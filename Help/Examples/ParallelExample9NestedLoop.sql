#beginMt C:\sqls\log.html
    #defaultManyThreadMode true
    #goStart -- main
        #box() : box1
        {
            #forBox(NPS_11.2.1.0.BETA) : forB1
            {
                for $row in [SELECT D.DATEKEY FROM JUST_DATA..DIMDATE D WHERE D.DATEKEY BETWEEN 20050101 AND 20050105 ORDER BY D.DATEKEY]
                #forBox(NPS_11.2.1.0.BETA) : forB2|$row[0]
                {
                    for $rowA in [
                        SELECT row_number() over (order by a.AUTOMATICRESPONSES ) aa1
                        FROM JUST_DATA..FACTCALLCENTER a
                        LIMIT 500
                                ]
                    #python() : python|$rowA[0]|$row[0]
                    {
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="row[0] = $row[0] | rowA[0] = $rowA[0]")
                        a.pack()
                        root.mainloop()
                    }
                    --#python() : python2|$rowA[0]|$row[0]
                    --{
                    --    from tkinter import *
                    --    root = Tk()
                    --    a = Label(root, text =" python2 row[0] = $row[0] | rowA[0] = $rowA[0]")
                    --    a.pack()
                    --    root.mainloop()
                    --}
                }
            }
        }
    #goEnd
#end
