#beginMt C:\sqls\log.html
    #usingStart
    #usingEnd
    #goStart -- main
        #box() : box1
        {
            #forBox(NPS_11.2.1.0.BETA) : forB1
            {
                for $row in [SELECT D.DATEKEY FROM JUST_DATA..DIMDATE D WHERE D.DATEKEY BETWEEN 20050101 AND 20050105 ORDER BY D.DATEKEY]
                #python() : p2|$row[0]
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="p2 in for loop $row[0]")
                    a.pack()
                    root.mainloop()
                }
            }
            #forBoxParallel(NPS_11.2.1.0.BETA) : forB2
            {
                #waitFor [forB1]
                for $row in [SELECT D.DATEKEY FROM JUST_DATA..DIMDATE D WHERE D.DATEKEY BETWEEN 20050101 AND 20050105 ORDER BY D.DATEKEY]
                #python() : p|$row[0]
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="p in for loop $row[0]")
                    a.pack()
                    root.mainloop()
                }
            }
        }
    #goEnd
#end
