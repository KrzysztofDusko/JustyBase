#beginMt C:\sqls\log.html
    #usingStart
    #usingEnd
    #goStart -- main
        #box() : box1
        {
            #python() : p1
            {
                from tkinter import *
                root = Tk()
                a = Label(root, text ="Hello World p1")
                a.pack()
                root.mainloop()
            }
            #box() : box2
            {
                #waitFor [p1]
                #python() : p2
                {
                    #waitFor [p3]
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="Hello World p2 in box2")
                    a.pack()
                    root.mainloop()
                }
                #python() : p3
                {
                    --#waitFor p2
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="Hello World p3 in box2")
                    a.pack()
                    root.mainloop()
                }
                #box() : box3
                {
                    #python() : p4
                    {
                        #waitFor [p2]
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="Hello World p4 in box3")
                        a.pack()
                        root.mainloop()
                    }
                    #python() : p41
                    {
                        #waitFor [p4]
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="Hello World p41 in box3")
                        a.pack()
                        root.mainloop()
                    }
                    #python() : p42
                    {
                        #waitFor [p4]
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="Hello World p42 in box3")
                        a.pack()
                        root.mainloop()
                    }
                }
                #box() : box4
                {
                    #waitFor [box3]
                    #python() : p5
                    {
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="Hello World p5 in box4")
                        a.pack()
                        root.mainloop()
                    }
                }
            }
            #box() : box5
            {
                #waitFor [box2]
                #python() : final
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="final")
                    a.pack()
                    root.mainloop()
                }
            }
        }
    #goEnd
#end