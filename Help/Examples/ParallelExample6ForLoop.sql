#beginMt C:\sqls\log.html
    #usingStart
        #text $scripthPath : 10
        #text $par1 : 11
        #text $par2 : 0
    #usingEnd
    #goStart -- main
        #box() : box1
        {
            #forBox(NPS_11.2.1.0.BETA) : forB1
            {
                for $row in [SELECT D.DATEKEY FROM JUST_DATA..DIMDATE D WHERE D.DATEKEY BETWEEN 20050101 AND 20050103 ORDER BY D.DATEKEY]
                #python() : python|$row[0]
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="python|$row[0]")
                    a.pack()
                    root.mainloop()
                }
                #box() : innerBox|$row[0]
                {       
                    #waitFor [python|$row[0]]
                    #python() : innerPython|$row[0]
                    {
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="innerPython|$row[0]")
                        a.pack()
                        root.mainloop()
                    }
                    #python() : innerPython2|$row[0]
                    {
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="innerPython2|$row[0]")
                        a.pack()
                        root.mainloop()
                    }
                }
                
            }
        }
    #goEnd
#end
