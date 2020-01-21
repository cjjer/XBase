using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ProtoBuf;


namespace XBase.Model
{
    [ProtoContract]
    [DataContract]
    public class ListResult<T>
    {
        [DataMember]
        [ProtoMember(1)]
        public int PageIndex { get; set; }
        [DataMember]
        [ProtoMember(2)]
        public int PageSize { get; set; }
        [DataMember]
        [ProtoMember(3)]
        public int Hit { get; set; }
        [DataMember]
        [ProtoMember(4)]
        public List<T> Items { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public string Msg { get; set; }


    }

}
