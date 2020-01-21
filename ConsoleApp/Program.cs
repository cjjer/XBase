using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyBusinessLib.Model;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            生成随机数据一万条();
            var find = MyBusinessLib.Objects.Catalogs.GetOneByName("名称3");
            Console.WriteLine(find.Id);

            var list = MyBusinessLib.Objects.Products.GetListByQuery(find.Id, 1, 5);
            Console.WriteLine("总数为{0}条",list.Hit);

            foreach (var i in list.Items)
            {
                Console.WriteLine(i.Name);
            }
            Console.ReadLine();
        }

        static void 生成随机数据一万条()
        {
            if (MyBusinessLib.Objects.Catalogs.GetCount() > 0) return;


            {
                for (int i = 0; i < 20; i++)
                {
                    var one = new Catalog
                    {
                        Name = "名称" + i
                    };
                    var hasone = MyBusinessLib.Objects.Catalogs.SaveOne(one);
                    if (hasone == null)
                    {
                        throw new Exception("检查数据库");
                    }

                    //List<Product> ls = new List<Product>();
                    for (int j = 0; j < 500; j++)
                    {
                        var po = new Product
                        {
                            CatalogId = hasone.Id,
                            Name = "产品名称" + j,
                            PriceVal = j,
                            IsValid = j % 3 == 0,
                            Addtime = XBase.Utility.Common.ConvertToTimestamp(DateTime.Now),
                            StateVal = j % 2 == 0 ? ProductState.INIT : ProductState.SHOW
                        };
                        MyBusinessLib.Objects.Products.SaveOne(po);
                        //skip one by one
                        //ls.Add(po);

                    }
                    ////批量添加暂时不支持bool类型的
                    //MyBusinessLib.Objects.Products.BulkInsert(ls);
                    Console.WriteLine("批量保存500条哦");
                }
            }
        }
    }
}
