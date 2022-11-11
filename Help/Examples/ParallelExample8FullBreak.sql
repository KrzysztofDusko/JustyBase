#beginMt C:\sqls\log.html
    #usingStart
    #text $par1 : 0
    #usingEnd
    #goStart -- main
        #box() : box1
        {
            #forBox(NPS_11.2.1.0.BETA) : forB1
            {
                for $row in [SELECT D.DATEKEY FROM JUST_DATA..DIMDATE D WHERE D.DATEKEY BETWEEN 20050101 AND 20050105 ORDER BY D.DATEKEY]
                #box() : innerBox|$row[0]
                {
                    if $row[0] = 20050103
                    #break() : break|$row[0]
                    {
                    }
                }
                #python() : python|$row[0]
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="python|$row[0]")
                    a.pack()
                    root.mainloop()
                }
            }
        }
    #goEnd
#end
