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
    public class ProductService : XBase.DAO.RepositoryDao<Product>
    {
        public Product SaveOne(Product one)
        {
            if (one == null) return null;
           
            this.AddOrUpdate(one, true, DBAction.自动);
            return one;
        }

        public int DeleteOne(long id)
        {
            return this.DeleteOne(id);
        }

        public int UpdatePriceVal(long id)
        {
            Hashtable set = new Hashtable(2);
            set["StateVal"] = ProductState.SHOW;
            set["PriceVal+"] = 2;

            return this.WUpdate(set, id);
        }
        public Product GetValidOne(long id, ProductState state)
        {
            Hashtable ht = new Hashtable(2);
            ht["Id"] = id;
            ht["StateVal"] = state;
            return GetOne(ht);
        }
        public ListResult<Product> GetListByQuery( string catId, int page, int pagesize)
        {
            var result = new ListResult<Product>() { Items = new List<Product>() };
            page = Math.Max(1, page);
            if (pagesize < 1) return result;

            result.PageIndex = page;
            result.PageSize = pagesize;

            var fromTable = this.DbTable + " a ";
            Hashtable htp = new Hashtable(2);
            htp["IsValid"] = true;
            htp["CatalogId"] = catId;
            var cols = "*";
            var countAll = GetCount( htp, fromTable);
            if (countAll > 0)
            {
                result.Hit = countAll;
                result.Items = ToList<Product>(htp,"Id desc", page, pagesize, cols, fromTable);
            }
            
            return result;
        }


    }
}
