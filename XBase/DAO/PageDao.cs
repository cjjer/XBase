using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace XBase.DAO
{
    [DataContract]
    public class PageDao<T> where T : class
    {
        [DataMember]
        public int PageIndex { get; set; }
        [DataMember]
        public int PageSize { get; set; }
        [DataMember]
        public List<T> Item { get; set; }
        [DataMember]
        public int RecordCount { get; set; }
    }
}
