#beginMt C:\sqls\log.html
    #usingStart
    #usingEnd
    #goStart -- main
        #box() : box1
        {
            #forBox(NPS_11.2.1.0.BETA) : forB1
            {
                for $row in [SELECT D.DATEKEY FROM JUST_DATA..DIMDATE D WHERE D.DATEKEY BETWEEN 20050101 AND 20050103 ORDER BY D.DATEKEY]
                #python() : 1|$row[0]
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="1|$row[0]")
                    a.pack()
                    root.mainloop()
                }
                #box() : boxForBreak|$row[0]
                {
                    if $row[0] = 20050102
                    #python() : 1_5|$row[0]
                    {
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="1_5|$row[0]")
                        a.pack()
                        root.mainloop()
                    }
                    #breakFor() : breakFor|$row[0]
                    {
                    }
                }
                #python() : 2|$row[0]
                {
                    #waitFor [boxForBreak|$row[0]]
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="2|$row[0]")
                    a.pack()
                    root.mainloop()
                }
            }
            #python() : _final
            {
                #waitFor [forB1]
                from tkinter import *
                root = Tk()
                a = Label(root, text ="_final")
                a.pack()
                root.mainloop()
            }
        }
    #goEnd
#end
