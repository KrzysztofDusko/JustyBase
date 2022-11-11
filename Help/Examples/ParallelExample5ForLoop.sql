#beginMt C:\sqls\log.html
    #usingStart
    #usingEnd
    #goStart -- main
        #box() : box1
        {
            #forBox(NPS_11.2.1.0.BETA) : forB1
            {
                #waitFor [pX]
                for $row in [SELECT D.DATEKEY FROM JUST_DATA..DIMDATE D WHERE D.DATEKEY BETWEEN 20050101 AND 20050102 ORDER BY D.DATEKEY]
                #python() : p2|$row[0]
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="p2 in for loop $row[0]")
                    a.pack()
                    root.mainloop()
                }
                #python() : p3|$row[0]
                {
                    #waitFor [p2|$row[0]]
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="p3 in for loop $row[0]")
                    a.pack()
                    root.mainloop()
                }
                #box() : innerBox|$row[0]
                {
                    #python() : innerBoxPython|$row[0]
                    {
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="innerBoxPython in for loop $row[0]")
                        a.pack()
                        root.mainloop()
                    }
                }
            }
            #python() : pX
            {
                from tkinter import *
                root = Tk()
                a = Label(root, text ="pX - Start")
                a.pack()
                root.mainloop()
            }
            #python() : pX1
            {
                #waitFor [forB1]
                from tkinter import *
                root = Tk()
                a = Label(root, text ="pX1 - after for loop")
                a.pack()
                root.mainloop()
            }
            #forBoxParallel(NPS_11.2.1.0.BETA) : forB2
            {
                #waitFor [pX1]
                for $row in [SELECT 'XYZ' 
                    UNION ALL SELECT 'ABC']
                #python() : p4|$row[0]
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="p4 in parallel for loop $row[0]")
                    a.pack()
                    root.mainloop()
                }
                #python() : p5|$row[0]
                {
                    #waitFor [p4|$row[0]]
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="p5 in parallel for loop  $row[0]")
                    a.pack()
                    root.mainloop()
                }
                #python() : p6|$row[0]
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="p6 in parallel for loop  $row[0]")
                    a.pack()
                    root.mainloop()
                }
            }
        }
    #goEnd
#end
