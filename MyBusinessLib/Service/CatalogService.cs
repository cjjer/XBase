using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyBusinessLib.Model;
using XBase.Model;

namespace MyBusinessLib.Service
{
    public class CatalogService : XBase.DAO.RepositoryDao<Catalog>
    {
        public int GetCount()
        {
            Hashtable hq = new Hashtable(1);
            return GetCount(hq);
        }
        public Catalog GetOneByName(string name)
        {
            Hashtable hq = new Hashtable(1);
            hq["Name"] = name;
            return GetOne(hq);
            
        }
        public Catalog SaveOne(Catalog one)
        {
            if (one == null) return null;
            var actX = DBAction.更新;
            if (string.IsNullOrEmpty(one.Id))
            {
                one.Id = GetSafeGuid(true,10);
                actX = DBAction.插入;
            }
            else if(!this.HasId(one.Id))
            {
                actX = DBAction.插入;
            }
            this.AddOrUpdate(one, true, actX);
            return one;
        }
    }
}
