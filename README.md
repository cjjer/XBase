# XBase


## 简要介绍

`XBase <http://www.xiaozaixiao.com/>` 是c# 语言完成的对数据库的底层包装库.
通过使用她，你可以简明扼要的面向业务对象编程很少通过直接面向数据库操作，当然你还是需要懂数据库.
她是一个非常轻量级的框架，很小，比常见的类似类库都小.
当前默认已经支持了MYSQL,MSSQL两种常见的数据，当然你可以容易的扩展自己的数据库清单，例如sqllite等.


## 使用方法

### 类库引用

目前最低语言支持的是···`c#`···`.net framework 4.0`,如果你使用的是低于此版本的，可以通过修改很少的代码自行完成集成，
或者直接升级framework版本吧.

  * 引用了 `MySql.Data` 连接mysql数据库.
  * 引用了 `protobuf-net` 序列化对象，主要是搭建脚手架的时候给api服务用，框架本身并没有用到.


### 添加/更新 单个对象

通过使用 ``AddOrUpdate``方法添加单个对象

```
        public Product SaveOne(Product one)
        {
            if (one == null) return null;
           
            this.AddOrUpdate(one, true, DBAction.自动);
            return one;
        }
```
这种方式适合pk是自增的数据结构，如果非自增结构，如果添加单个对象用自动模式，就会一直当然更新数据，这时候需要自行指定


```
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

```

类似这样的去新增或更新数据。

更新部分字段数据的时候提供了一个非常简单的方法：

```
        public int UpdatePriceVal(long id)
        {
            Hashtable set = new Hashtable(2);
            set["StateVal"] = ProductState.SHOW;
            set["PriceVal+"] = 2;

            return this.WUpdate(set, id);
        }
```


### 单个读取

通过使用 ``GetOne`` 或者 ``GetByPKValue``快速的读取单个对象

```
        public Catalog GetOneByName(string name)
        {
            Hashtable hq = new Hashtable(1);
            hq["Name"] = name;
            return GetOne(hq);
            
        }
```


### 列表读取

这个方法用的非常多，根据我们的经验，提供了非常多的重载， ``ToList``是主要方法名称。

一个常见的使用方法是，通过某些指定的条件，既需要获取到结果总数，也需要返回分页条件的记录总数：

```
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
```



## 删除数据

```
        public int DeleteOne(long id)
        {
            return this.DeleteOne(id);
        }
```


## 联系我们

### 微信公众号 


```
    $ 程序员666
```
