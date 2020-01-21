# XBase


## ��Ҫ����

`XBase <http://www.xiaozaixiao.com/>` ��c# ������ɵĶ����ݿ�ĵײ��װ��.
ͨ��ʹ����������Լ�����Ҫ������ҵ������̺���ͨ��ֱ���������ݿ��������Ȼ�㻹����Ҫ�����ݿ�.
����һ���ǳ��������Ŀ�ܣ���С���ȳ�����������ⶼС.
��ǰĬ���Ѿ�֧����MYSQL,MSSQL���ֳ��������ݣ���Ȼ��������׵���չ�Լ������ݿ��嵥������sqllite��.


## ʹ�÷���

### �������

Ŀǰ�������֧�ֵ��ǡ�����`c#`������`.net framework 4.0`,�����ʹ�õ��ǵ��ڴ˰汾�ģ�����ͨ���޸ĺ��ٵĴ���������ɼ��ɣ�
����ֱ������framework�汾��.

  * ������ `MySql.Data` ����mysql���ݿ�.
  * ������ `protobuf-net` ���л�������Ҫ�Ǵ���ּܵ�ʱ���api�����ã���ܱ���û���õ�.


### ���/���� ��������

ͨ��ʹ�� ``AddOrUpdate``������ӵ�������

```
        public Product SaveOne(Product one)
        {
            if (one == null) return null;
           
            this.AddOrUpdate(one, true, DBAction.�Զ�);
            return one;
        }
```
���ַ�ʽ�ʺ�pk�����������ݽṹ������������ṹ�������ӵ����������Զ�ģʽ���ͻ�һֱ��Ȼ�������ݣ���ʱ����Ҫ����ָ��


```
        public Catalog SaveOne(Catalog one)
        {
            if (one == null) return null;
            var actX = DBAction.����;
            if (string.IsNullOrEmpty(one.Id))
            {
                one.Id = GetSafeGuid(true,10);
                actX = DBAction.����;
            }
            else if(!this.HasId(one.Id))
            {
                actX = DBAction.����;
            }
            this.AddOrUpdate(one, true, actX);
            return one;
        }

```

����������ȥ������������ݡ�

���²����ֶ����ݵ�ʱ���ṩ��һ���ǳ��򵥵ķ�����

```
        public int UpdatePriceVal(long id)
        {
            Hashtable set = new Hashtable(2);
            set["StateVal"] = ProductState.SHOW;
            set["PriceVal+"] = 2;

            return this.WUpdate(set, id);
        }
```


### ������ȡ

ͨ��ʹ�� ``GetOne`` ���� ``GetByPKValue``���ٵĶ�ȡ��������

```
        public Catalog GetOneByName(string name)
        {
            Hashtable hq = new Hashtable(1);
            hq["Name"] = name;
            return GetOne(hq);
            
        }
```


### �б��ȡ

��������õķǳ��࣬�������ǵľ��飬�ṩ�˷ǳ�������أ� ``ToList``����Ҫ�������ơ�

һ��������ʹ�÷����ǣ�ͨ��ĳЩָ��������������Ҫ��ȡ�����������Ҳ��Ҫ���ط�ҳ�����ļ�¼������

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



## ɾ������

```
        public int DeleteOne(long id)
        {
            return this.DeleteOne(id);
        }
```


## ��ϵ����

### ΢�Ź��ں� 


```
    $ ����Ա666
```
