#beginMt C:\sqls\log.html
    #usingStart
    #text $par1 : 0
    #usingEnd
    #goStart -- main
        #box() : box1
        {
            #forBoxParallel(NPS_11.2.1.0.BETA) : forB1
            {
            for $row in [SELECT D.DATEKEY FROM JUST_DATA..DIMDATE D 
                        WHERE D.DATEKEY BETWEEN 20050101 AND 20050110 ORDER BY D.DATEKEY]
                #setFromRaw($par1_$row[0],$row[0]) : SET1|$row[0]
                {
                }
                #python() : wait|$row[0]
                {
                    #waitFor [SET1|$row[0]]
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="wait python $row[0]")
                    a.pack()
                    root.mainloop()
                }
                #python() : python|$row[0]
                {
                    #waitFor [wait|$row[0]]
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="python $par1_$row[0] $row[0]")
                    a.pack()
                    root.mainloop()
                }
            }
        }
    #goEnd
#end
