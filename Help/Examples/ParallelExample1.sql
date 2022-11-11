#beginMt C:\sqls\log.html
    #goStart -- main
        #box() : box1
        {
        #exportToFile(NPS_11.2.1.0.BETA,C:\sqls\xlsxFile.xlsx) : xlsx0
            {
                SELECT * FROM FACTPRODUCTINVENTORY LIMIT 2
            }
            #python() : py
            {
                from tkinter import *
                root = Tk()
                a = Label(root, text ="Python !")
                a.pack()
                root.mainloop()
            }     
            #exportToFile(NPS_11.2.1.0.BETA,C:\sqls\csv2.csv) : csv2
            {
                #waitFor [xlsx0]
                SELECT NEXT VALUE FOR  sequence1
            }
            #exportToFile(NPS_11.2.1.0.BETA,C:\sqls\csv3.csv) : csv3
            {
                #waitFor [csv4]
                SELECT * FROM FACTPRODUCTINVENTORY  LIMIT 5
            }
            #exportToFile(NPS_11.2.1.0.BETA,C:\sqls\csv4.csv) : csv4
            {
                #waitFor [xlsx0] and [py]
                SELECT * FROM FACTPRODUCTINVENTORY LIMIT 10000
            }
        }
    #goEnd
#end