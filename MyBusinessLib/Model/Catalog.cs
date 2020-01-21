using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using XBase.DAO;
using XBase.Model;

namespace MyBusinessLib.Model
{
    [SQLSpring("Catalog", "Id", "(|Id|Name|)", IdentityPK = false)]
    [ProtoContract]
    public class Catalog : Article
    {
        [ProtoMember(1)]
        public string Id { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }


    }
  
}
