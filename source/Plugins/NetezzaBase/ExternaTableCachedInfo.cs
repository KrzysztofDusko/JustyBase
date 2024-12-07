using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustyBase.Services.Database;
public record ExternaTableCachedInfo
{
    public string DATAOBJECT { get; set; }
    public string DELIMITER { get; set; }
    public string ENCODING { get; set; }
    public string TIMESTYLE { get; set; }
    public string REMOTESOURCE { get; set; }
    public long? SKIPROWS { get; set; }
    public long? MAXERRORS { get; set; }
    public string ESCAPECHAR { get; set; }
    public string DECIMALDELIM { get; set; }
    public string LOGDIR { get; set; }
    public string QUOTEDVALUE { get; set; }
    public string NULLVALUE { get; set; }
    public bool? CRINSTRING { get; set; }
    public bool? TRUNCSTRING { get; set; }
    public bool? CTRLCHARS { get; set; }
    public bool? IGNOREZERO { get; set; }
    public bool? TIMEEXTRAZEROS { get; set; }
    public Int16? Y2BASE { get; set; }
    public bool? FILLRECORD { get; set; }
    public string COMPRESS { get; set; }
    public bool? INCLUDEHEADER { get; set; }
    public bool? LFINSTRING { get; set; }
    public string DATESTYLE { get; set; }
    public string DATEDELIM { get; set; }
    public string TIMEDELIM { get; set; }
    public string BOOLSTYLE { get; set; }
    public string FORMAT { get; set; }
    public int? SOCKETBUFSIZE { get; set; }
    public string RECORDDELIM { get; set; }
    public Int64? MAXROWS { get; set; }
    public bool? REQUIREQUOTES { get; set; }
    public string RECORDLENGTH { get; set; }
    public string DATETIMEDELIM { get; set; }
    public string REJECTFILE { get; set; }
}
