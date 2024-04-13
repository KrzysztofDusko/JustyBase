#beginMt C:\sqls\log.html
    #goStart -- main
    #box() : box1
    {
        #forBox(NPS_11.2.1.0.BETA) : forB1
        {
            for $row in [SELECT D.DATEKEY FROM JUST_DATA..DIMDATE D WHERE D.DATEKEY BETWEEN 20050101 AND 20050103 ORDER BY D.DATEKEY]
            #box() : box2|$row[0]
            {
                #python() : px|$row[0]
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="PX - $row[0]")
                    a.pack()
                    root.mainloop()
                }
            }
        }
    }
    #goEnd
#end