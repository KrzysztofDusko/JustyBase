# Just Data - Netezza oriented SQL Editor
Just Data is SQL editor. Mainly for Netezza, but DB2, Microsft SQL, Oracle, PosgreSQL are also supported.
## [Download](https://github.com/KrzysztofDusko/Just-Data/releases/tag/Current)
## Table of Contents
   - [Overview](#overview)
   - [Instalation](#Instalation)
   - [Features](#features)
     - [Import and Export](#import-and-export)
     - [Code hints](#code-hints)
     - [Scripting](#scripting)
     - [Supported DB's](#supported-dbs)
     - [Samples](#samples)
   - [FAQs](#faqs)
   - [Source Code](#sourcecode)

## Overview
Just Data instaler, issues and support page
![alt text](https://github.com/KrzysztofDusko/Just-Data/blob/main/pictures/pic1.png)
![alt text](https://github.com/KrzysztofDusko/Just-Data/blob/main/pictures/pic2.png)

## Instalation
*  https://github.com/KrzysztofDusko/Just-Data/blob/main/Installers/Offline/Output/SetupNetIncluded.7z

### Recorded instalation
* https://youtu.be/XCf-0uHXfY4
* (NET included) https://youtu.be/fFqa-Cq16Qc
* net 5 without admin privileges https://youtu.be/_NH1fQZa6Ac

## Features
### Import and Export
* https://youtu.be/XWsvLrp2ghQ
* https://youtu.be/8D3m8hdOyKE

### Code hints
* https://youtu.be/pS8R9q2asfE

### Scripting
* https://youtu.be/IMrX2PAq96g
* Examples
   * Basic Sample 
   (live log to C:\sqls\log.html)
   ```sql
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
   ```
   * CLI syntax
   ```code
       justdata.exe script "C:\sqls\pivotExample.sql" "C:\sqls\log.html"
   ```
   * Advanced Sample
   https://github.com/KrzysztofDusko/Just-Data/issues/1
   * Write to existing xlsx file
   ```sql
       #exportToFileAdvanced(MsSqlLocal) : taskName
       {
           #type xlsx
           #path  C:\sqls\pivotTableExample.xlsx     
           #tabname source
           #sqlStart
               SELECT top 3 a.* FROM dbo.DimProductSubcategory a
           #sqlEnd
       }
   ```
   * Refresh pivot table example
   ```sql
        #exportToFileAdvanced(MsSqlLocal) : taskName
        {
            #type xlsx
            #path  C:\sqls\pivotTableExample.xlsx     
            #tabname source -- worksheet name
            #pivotTableTabName pivotTable -- pivot table worksheet name
            #pivotTableName pivotTable1 --pivot table na
            --#startCell A1 -- default A1, not required
            --#forceRefresh true -- true/false, default true, not required
            #sqlStart
                SELECT a.* FROM dbo.DimProductSubcategory a
            #sqlEnd
        }
   ```

   * Variables
   ```sql
        #beginMt C:\sqls\log.html
            #usingStart
            #text $par1 : 0
            #usingEnd
            #goStart -- main
                #box() : box1
                {
                    #forBoxParallel(NPS_11.2.1.0.BETA) : forB1
                    {
                    for $row in [SELECT D.DATEKEY FROM JUST_DATA..DIMDATE D 
                                WHERE D.DATEKEY BETWEEN 20050101 AND 20050110 ORDER BY D.DATEKEY]
                        #setFromRaw($par1_$row[0],$row[0]) : SET1|$row[0]
                        {
                        }
                        #python() : wait|$row[0]
                        {
                            #waitFor [SET1|$row[0]]
                            from tkinter import *
                            root = Tk()
                            a = Label(root, text ="wait python $row[0]")
                            a.pack()
                            root.mainloop()
                        }
                        #python() : python|$row[0]
                        {
                            #waitFor [wait|$row[0]]
                            from tkinter import *
                            root = Tk()
                            a = Label(root, text ="python $par1_$row[0] $row[0]")
                            a.pack()
                            root.mainloop()
                        }
                    }
                }
            #goEnd
        #end

   ```
   * Complex example 1
	lopp, if statement, nested blocks
   ```sql
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

   ```
   
### Supported DB's
* https://youtu.be/xJOMBVqfZIg

### Samples
 * https://github.com/KrzysztofDusko/Just-Data/tree/main/Help/Examples

## FAQs
### 1. Question 
Answer 1

## Source Code
Source code can be provided for request (e.g. for security reasons).
