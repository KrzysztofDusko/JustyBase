#beginMt C:\sqls\log.html
    #usingStart
    #usingEnd
    #goStart
        #box() : box1
        {
            #box() : box2
            {
                #box() : box3
                {
                    #python() : p1
                    {
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="Hello World p1")
                        a.pack()
                        root.mainloop()
                    }
                }
                #python() : p2
                {
                    #waitFor [p3]
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="Hello World p2")
                    a.pack()
                    root.mainloop()
                }
            }
            #python() : p3
            {
                #waitFor [p1]
                from tkinter import *
                root = Tk()
                a = Label(root, text ="Hello World p3")
                a.pack()
                root.mainloop()
            }
            
            #box() : box7
            {
                #waitFor [p3]
                #python() : p4
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="Hello World p4 in box7")
                    a.pack()
                    root.mainloop()
                }
                #python() : p5
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="Hello World p5 in box7")
                    a.pack()
                    root.mainloop()
                }
            }
            
        }
    #goEnd
#end