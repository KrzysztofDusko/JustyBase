#beginMt C:\sqls\log.html
    #usingStart
    #usingEnd
    #goStart -- main
        #box() : box1
        {
            #box() : boxA
            {
                #python() : p1
                {
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="Hello World p1 in boxA")
                    a.pack()
                    root.mainloop()
                }
            }
            #box() : box2
            {
                #waitFor [boxA]
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
                    from tkinter import *
                    root = Tk()
                    a = Label(root, text ="Hello World p3 in box2")
                    a.pack()
                    root.mainloop()
                }
                #box() : boxC
                {
                    #waitFor [p2]
                    #python() : pC
                    {
                        #waitFor [p2]
                        from tkinter import *
                        root = Tk()
                        a = Label(root, text ="Hello World pC in boxC")
                        a.pack()
                        root.mainloop()
                    }
                }
            }
        }
    #goEnd
#end