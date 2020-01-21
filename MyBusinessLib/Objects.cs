using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBusinessLib
{
    public class Objects
    {
        public static Service.ProductService Products = new Service.ProductService();
        public static Service.CatalogService Catalogs = new Service.CatalogService();
    }
}
