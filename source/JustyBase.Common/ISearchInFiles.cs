using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustyBase.PluginDatabaseBase.AnotherContracts;

public interface ISearchInFiles
{
    bool IsWordInFile(string path, string toSearch, bool searchInSqlComments);
    bool IsWholeWordInFile(string path, string toSearch, bool searchInSqlComments);
}
