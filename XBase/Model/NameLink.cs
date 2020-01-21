using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ProtoBuf;

namespace XBase.Model
{
    [ProtoContract]
    public class NameLink
    {
        [ProtoMember(1)]
        public string Name
        {
            get;
            set;
        }
        [ProtoMember(2)]
        public string Permalink
        {
            get;
            set;
        }
        [ProtoMember(3)]
        public string Ext
        {
            get;
            set;
        }
    }
}
