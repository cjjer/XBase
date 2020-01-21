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
    [SQLSpring("Product", "Id", "(|Id|Addtime|Name|CatalogId|IsValid|StateVal|PriceVal|)", IdentityPK = true)]
    [ProtoContract]
    public class Product : Article
    {
        [ProtoMember(1)]
        public long Id { get; set; }

        [ProtoMember(2)]
        public long Addtime { get; set; }
        [ProtoMember(3)]
        public string Name { get; set; }
        [ProtoMember(4)]
        public string CatalogId { get; set; }
        [ProtoMember(5)]
        public bool IsValid { get; set; }
        [ProtoMember(6)]
        public ProductState StateVal { get; set; }
        [ProtoMember(7)]
        public decimal PriceVal { get; set; }

    }
    public enum ProductState:byte
    {
        INIT = 1, SHOW = 2, LOCKED = 4, OFFLINE = 8
    }

}
