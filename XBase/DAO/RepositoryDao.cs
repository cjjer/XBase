using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Data;
using System.Reflection;
using SqlHelper = XBase.SqlHelper;
using XBase.SqlHelper;
using XBase.DBHelper;
using XBase.Model;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using XBase.Utility;

namespace XBase.DAO
{
    public class RepositoryDao<T> where T : class, new()
    {
        public event EventHandler Finished;
        #region private
        private static AbsDBHelper _RunDB;
        protected static AbsDBHelper RunDB
        {
            get
            {
                if (_RunDB != null) return _RunDB;
                lock (olock)
                {
                    if (_RunDB != null) return _RunDB;
                    var dbTypeConnection = System.Configuration.ConfigurationManager.AppSettings["DbConnTypeName"];
                    if (!string.IsNullOrWhiteSpace(dbTypeConnection))
                    {
                        if (!dbTypeConnection.Contains(".") && !dbTypeConnection.Contains(","))
                        {
                            dbTypeConnection = string.Format("XBase.DBHelper.{0}DBHelper,XBase", dbTypeConnection);
                        }
                        _RunDB = Activator.CreateInstance(Type.GetType(dbTypeConnection, true, true)) as AbsDBHelper;
                        if (dbTypeConnection.IndexOf("mssql", StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            MSSQL_NOLOCK = " with(nolock) ";
                            User_MSSQL = true;
                        }
                    }
                    else
                    {
                        _RunDB = new MSSQLDBHelper();
                        MSSQL_NOLOCK = " with(nolock) ";
                        User_MSSQL = true;
                    }

                }
                return _RunDB;
            }
        }

        private string _DbConn = null;
        protected string DbConnectionString
        {
            get
            {
                if (_DbConn == null)
                {
                    string names = typeof(T).Namespace;
                    if (String.IsNullOrWhiteSpace(names)) return null;
                    _DbConn = System.Configuration.ConfigurationManager.AppSettings[names.Split('.')[0] + "_DbConnString"];

                }
                return _DbConn;
            }
            set { _DbConn = value; }
        }
        private static string MSSQL_NOLOCK = string.Empty;
        private static bool User_MSSQL = false;
        protected virtual void OnFinished(EventArgs e)
        {
            if (Finished != null)
            {
                Finished(this, e);
            }
        }
        #endregion

        public static string GetNolockMySql(string sql)
        {
            return string.Format("{0};{1};{2};",
                "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED",
                sql.Trim().Trim(';'),
                "COMMIT");
        }
        public int Update(T obj, Hashtable ht)
        {
            if (obj == null) return 0;
            if (ht == null || ht.Count < 1) return 0;

            Type t = obj.GetType();

            var _T_PkName = PkName;
            var _T_TableName = DbTable;
            if (String.IsNullOrEmpty(_T_PkName) || String.IsNullOrEmpty(_T_TableName)) return 0;
            PropertyInfo pk = t.GetProperty(_T_PkName);
            object pkvalue = pk.GetValue(obj, null);

            if (pkvalue == null)
            {
                return 0;
            }
            Hashtable htupdate = new Hashtable();
            htupdate[_T_PkName] = pkvalue;

            string sql = String.Format("update {0} set ", _T_TableName);
            string updatesql = "";
            foreach (string x in ht.Keys)
            {
                updatesql += "," + x + "=@" + x;
                htupdate[x] = ht[x];
            }
            updatesql = updatesql.Trim(',');
            sql += updatesql;
            sql += " where " + _T_PkName + "=@" + _T_PkName;
            return RunDB.Execute(sql, htupdate, DbConnectionString);
        }
        private static SQLSpring __Spring = null;
        private static SQLSpring Spring
        {
            get
            {
                if (__Spring != null)
                {
                    return __Spring;
                }
                Type t = typeof(T);
                //NEED CACHE
                object[] roles = t.GetCustomAttributes(typeof(SQLSpring), true);
                if (roles == null || roles.Length < 1)
                {
                    __Spring = new SQLSpring(t.Name, "Id", null, true);
                }
                else
                {
                    __Spring = roles[0] as SQLSpring;
                }

                return __Spring;
            }
        }
        public object AddOrUpdate(T obj, params object[] paras)
        {
            Type t = obj.GetType();
            //NEED CACHE
            var sp = Spring;
            string TableName = sp.TableName, PKName = sp.PKName, Columns = sp.Columns;
            bool 自增编号 = sp.IdentityPK;

            object temp操作 = null;
            DBAction 插入数据 = DBAction.插入;

            if (paras != null && paras.Length > 0)
            {
                temp操作 = paras.FirstOrDefault(x => x.GetType().Equals(typeof(DBAction)));
                if (temp操作 != null)
                {
                    插入数据 = (DBAction)temp操作;
                }
            }
            PropertyInfo pk = t.GetProperty(PKName);
            if (temp操作 == null || 插入数据.Equals(DBAction.自动))
            {
                if (pk != null && pk.CanWrite)
                {
                    object vpk = pk.GetValue(obj, null);
                    //var vpk = PropertyCallAdapterProvider<T>.GetInstance(PKName).InvokeGet(obj);

                    if (vpk != null)
                    {
                        if (!(vpk.ToString().Equals("0")))
                        {
                            插入数据 = DBAction.更新;

                        }
                        else
                        {
                            插入数据 = DBAction.插入;
                        }
                    }
                }
            }

            string sql = "";
            Hashtable ht = new Hashtable();

            Dictionary<string, string> _扩展叠加字段 = null;
            if (paras != null)
            {
                _扩展叠加字段 = (Dictionary<string, string>)paras.FirstOrDefault(x => x.GetType().Equals(typeof(Dictionary<string, string>)));
            }
            if (_扩展叠加字段 == null) _扩展叠加字段 = new Dictionary<string, string>();
            var hashAppendKey = obj.GetHashCode().ToString();
            foreach (PropertyInfo pi in t.GetProperties())
            {
                string piName = pi.Name;//不做处理，注意大小写 
                object v = pi.GetValue(obj, null);
                try
                {
                    if ((string.IsNullOrWhiteSpace(Columns) || Columns.IndexOf(piName.GetForkColumn()) != -1))
                    {
                        bool 赋值 = true;
                        if (v != null && v.GetType().Equals(typeof(DateTime)))
                        {
                            //对日期联系的做特殊处理       
                            if (((DateTime)v).Year < 1753) 赋值 = false;
                        }
                        if (赋值)
                        {
                            if (piName.Equals(PKName, StringComparison.CurrentCultureIgnoreCase))
                            {
                                //是主键
                                if (插入数据.Equals(DBAction.插入) && v != null && v.ToString() != "0")
                                {
                                    //是插入,有值，OK
                                    sql += piName + ",";
                                }
                            }
                            else
                            {
                                //OK
                                sql += piName + ",";
                            }
                            var tmpt = pi.PropertyType;
                            if (!tmpt.IsValueType && tmpt != typeof(string))
                            {
                                v =  JSONHelper.ToJSON(v);
                            }
                            ht[piName + hashAppendKey] = v;

                        }
                    }
                }
                catch (Exception ep)
                {
                    Console.WriteLine(ep.ToString());
                }
            }
            if (ht == null || ht.Count < 1) return null;
            foreach (var xi in _扩展叠加字段)
            {
                var mi = xi.Key + hashAppendKey;
                if (ht.ContainsKey(mi)) continue;//不允许更新已经被修改的值
                sql += xi.Key + ",";
                ht[mi] = xi.Value;
            }
            sql = sql.Trim(',');
            if (string.IsNullOrEmpty(sql)) return null;
            if (插入数据.Equals(DBAction.插入))
            {
                var preargs = string.Join(",", from xi in sql.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) select "@" + xi.Trim() + obj.GetHashCode());
                sql = String.Format(" insert  into {0} ({1}) values ({2}) ", TableName, sql, preargs);
            }
            else
            {
                var preargs = string.Join(",", from xi in sql.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                               select xi.Trim() + "=@" + xi.Trim() + obj.GetHashCode());
                sql = String.Format(" update {0} set {1} where {2}=@{2}{3}", TableName, preargs, PKName, obj.GetHashCode());
            }
            bool 就地执行 = false;

            if (paras != null)
            {
                for (int i = 0; i < paras.Length; i++)
                {
                    object o = paras[i];
                    if (null == o) continue;
                    if (o.GetType().Equals(typeof(StringBuilder)))
                    {
                        StringBuilder sb = o as StringBuilder;
                        if (i.Equals(0))
                        {
                            sb.AppendFormat(" {0}", sql);
                        }
                        else
                        {
                            sb.Insert(0, sql);
                        }
                        sql = sb.ToString();
                    }

                    else if (o.GetType().Equals(typeof(Hashtable)))
                    {
                        Hashtable newht = o as Hashtable;
                        foreach (DictionaryEntry de in ht)
                        {
                            if (!newht.ContainsKey(de.Key))
                            {
                                newht.Add(de.Key, de.Value);
                            }
                        }
                        ht = newht;
                    }
                    else if (o.GetType().Equals(typeof(bool)))
                    {
                        bool 是否执行 = (bool)o;
                        if (是否执行) 就地执行 = true;
                    }
                }
            }
            if (就地执行)
            {
                bool 需返回值 = false;
                //直接执行
                PropertyInfo pi = t.GetProperty(PKName);
                if (自增编号 && 插入数据.Equals(DBAction.插入))
                {
                    object defaultvalue = pi.GetValue(obj, null);
                    if (defaultvalue == null || defaultvalue.ToString().Equals("0"))
                    {
                        需返回值 = true;
                        sql += " ; " + RunDB.GetAutoCols(PKName + obj.GetHashCode().ToString());
                    }
                }
                DataSet ds = null;
                if (需返回值)
                {
                    ds = RunDB.GetDataSet(sql, ht, DbConnectionString);
                    if (!Common.IsNull(ds))
                    {
                        if (插入数据.Equals(DBAction.插入) && ds.Tables.Count > 0 && !Common.IsNull(ds.Tables[ds.Tables.Count - 1]))
                        {
                            if (ds.Tables[ds.Tables.Count - 1].Columns.Contains(PKName + obj.GetHashCode().ToString()))
                            {
                                if (pi != null && pi.CanWrite)
                                {
                                    var od = ds.Tables[ds.Tables.Count - 1].Rows[0][PKName + obj.GetHashCode().ToString()];
                                    var re_id_obj = od.ToString();

                                    if (pi.PropertyType == typeof(int))
                                    {
                                        pi.SetValue(obj, int.Parse(re_id_obj), null);
                                    }
                                    else if (pi.PropertyType == typeof(long))
                                    {
                                        pi.SetValue(obj, long.Parse(re_id_obj), null);
                                    }
                                    else if (pi.PropertyType == typeof(ulong))
                                    {
                                        pi.SetValue(obj, ulong.Parse(re_id_obj), null);
                                    }
                                    else if (pi.PropertyType == typeof(uint))
                                    {
                                        pi.SetValue(obj, uint.Parse(re_id_obj), null);
                                    }
                                    else
                                    {
                                        pi.SetValue(obj, od, null);
                                    }

                                }
                            }
                        }
                    }
                }
                else
                {
                    RunDB.Execute(sql, ht, DbConnectionString);
                }
                return ds;
            }
            return new ArrayList() { sql, ht };
        }


        public static RepositoryDao<T> Me = new RepositoryDao<T>();
        public PageDao<T> GetPageDaoList(Hashtable ht, string orderby, int pageindex, int pagesize, string returncol = "*")
        {
            pageindex = Math.Max(1, pageindex);
            pagesize = Math.Max(1, pagesize);
            PageDao<T> ls = new PageDao<T>();
            ls.PageIndex = pageindex;
            ls.PageSize = pagesize;
            ls.RecordCount = this.GetCount(ht);
            if (!((ls.PageIndex - 1) * ls.PageSize > ls.RecordCount || ls.RecordCount < 1))
            {
                ls.Item = this.ToList(ht, orderby, pageindex, pagesize, returncol);
            }
            return ls;
        }

        public int Execute(string sql, Hashtable ht = null)
        {
            return RunDB.Execute(sql, ht, DbConnectionString);
        }
        /// <summary>
        /// 得到第一行的第一个记录
        /// </summary>
        /// <param name="sqlWhere"></param>
        /// <param name="ht"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public object GetObject(string sql, Hashtable ht)
        {
            //string sql = String.Format("select {0} from {1}   ", col, DbTable);
            //if (!String.IsNullOrEmpty(sqlWhere) && sqlWhere.Trim().IndexOf(" ") > 0)
            //{
            //    sqlWhere = sqlWhere.Trim();
            //    if (sqlWhere.StartsWith("select ", StringComparison.OrdinalIgnoreCase))
            //    {
            //        sql = sqlWhere;
            //    }
            //    else
            //    {
            //        if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
            //        sql += sqlWhere;

            //    }
            //}
            //else if (String.IsNullOrWhiteSpace(sqlWhere))
            //{
            //    Hashtable htWhere = new Hashtable();
            //    string sqlWhere2 = IOHelper.GetSqlWhere(ht, htWhere);
            //    if (!String.IsNullOrEmpty(sqlWhere2) && sqlWhere2.Trim().IndexOf(" ") > 0)
            //    {
            //        sqlWhere = sqlWhere2.Trim();

            //        if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
            //        sql += sqlWhere;
            //    }
            //    ht = htWhere;

            //}

            return RunDB.GetObject(sql, ht, DbConnectionString);
        }

        /// <summary>
        /// 得到汇总
        /// </summary>
        /// <param name="sqlWhere">查询条件</param>
        /// <param name="ht">查询序列值</param>
        /// <returns></returns>

        public int GetCount(string sqlWhere, Hashtable ht, String fromtable = null)
        {
            if (string.IsNullOrWhiteSpace(fromtable)) fromtable = DbTable + MSSQL_NOLOCK;
            string sql = "select count(0) from " + fromtable;


            if (!String.IsNullOrEmpty(sqlWhere) && (sqlWhere.Trim().IndexOf(" ") > 0 || sqlWhere.Length > 5))
            {
                sqlWhere = sqlWhere.Trim();
                if (sqlWhere.StartsWith("select ", StringComparison.OrdinalIgnoreCase))
                {
                    sql = sqlWhere;
                }
                else
                {
                    if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
                    sql += sqlWhere;
                }
            }
            return RunDB.GetCount(sql, ht, DbConnectionString);
        }

        /// <summary>
        /// 得到汇总
        /// </summary>
        /// <param name="ht">查询条件->值</param>
        /// <returns></returns>
        public int GetCount(Hashtable ht, string fromtable = null)
        {
            if (string.IsNullOrWhiteSpace(fromtable)) fromtable = DbTable + MSSQL_NOLOCK;
            Hashtable htWhere = new Hashtable();
            string sql = "select count(0) from " + fromtable;
            string sqlWhere = IOHelper.GetSqlWhere(ht, htWhere, User_MSSQL);
            if (!String.IsNullOrEmpty(sqlWhere) && (sqlWhere.Trim().IndexOf(" ") > 0 || sqlWhere.Length > 4))
            {
                sqlWhere = sqlWhere.Trim();

                if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
                sql += sqlWhere;
            }
            return RunDB.GetCount(sql, htWhere, DbConnectionString);
        }
        public int WUpdate(Hashtable setht, Hashtable wht)
        {
            if (setht == null || wht == null) return 0;
            string sql = IOHelper.GetSqlUpdate(DbTable, setht, wht);
            return RunDB.ExecuteNonQuery(sql, setht, DbConnectionString);
        }

        public int WUpdate(Hashtable setht, object pkval)
        {
            if (setht == null || setht.Count<1 || pkval == null) return 0;
            var wht = new Hashtable();
            wht[Spring.PKName] = pkval;
            string sql = IOHelper.GetSqlUpdate(DbTable, setht, wht);
            return RunDB.ExecuteNonQuery(sql, setht, DbConnectionString);
        }

        public T GetOne(string sql, Hashtable ht = null)
        {
            return GetOne<T>(sql, ht);
        }
        public Z GetOne<Z>(string sql, Hashtable ht = null)
        {
            var dr = RunDB.GetRow(sql, ht, DbConnectionString);
            if (dr == null) return default(Z);
            return FlushTo<Z>(dr);
        }

        public T GetOneOrder(Hashtable ht, string orderby)
        {
            return GetOneOrder<T>(ht, orderby);
        }
        public T GetOne(Hashtable ht)
        {
            return GetOneOrder<T>(ht, null);
        }
        public Z GetOneOrder<Z>(Hashtable ht, string orderby)
        {
            string fromTable = (DbTable + MSSQL_NOLOCK);
            Hashtable htWhere = new Hashtable();
            string sql = null;
            if (!User_MSSQL)
            {
                sql = "select  * from " + fromTable;
            }
            else
            {
                sql = "select top 1 * from " + fromTable;
            }
            string sqlWhere = IOHelper.GetSqlWhere(ht, htWhere, User_MSSQL);
            if (!String.IsNullOrEmpty(sqlWhere) && (sqlWhere.Trim().IndexOf(" ") > 0 || sqlWhere.Length > 4))
            {
                sqlWhere = sqlWhere.Trim();
                if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
                sql += sqlWhere;
            }
            if (!string.IsNullOrEmpty(orderby))
            {
                sql += " order by " + orderby;
            }
            if (!User_MSSQL)
            {
                sql += " limit 1";
            }
            return GetOne<Z>(sql, htWhere);
        }

        public Z GetOne<Z>(Hashtable ht)
        {
            return GetOneOrder<Z>(ht, null);
        }
        /// <summary>
        /// 得到分页后信息
        /// </summary>
        /// <param name="ht"></param>
        /// <param name="返回列"></param>
        /// <param name="排序条件"></param>
        /// <param name="当前页数"></param>
        /// <param name="每页条数"></param>
        /// <returns></returns>
        public List<T> ToList(Hashtable ht, string 排序条件, int 当前页数, int 每页条数, string 返回列 = "*")
        {
            Hashtable htWhere = new Hashtable();
            string sqlWhere = IOHelper.GetSqlWhere(ht, htWhere, User_MSSQL);
            if (!String.IsNullOrEmpty(sqlWhere) && (sqlWhere.Trim().IndexOf(" ") > 0 || sqlWhere.Length > 4))
            {
                sqlWhere = sqlWhere.Trim();
                if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
            }
            else
            {
                sqlWhere = "  ";
            }
            return this.ToList(sqlWhere, htWhere, 排序条件, 当前页数, 每页条数, 返回列);
        }



        public List<T> ToList(Hashtable ht, string 排序条件 = null)
        {
            Hashtable htWhere = new Hashtable();
            string sqlWhere = IOHelper.GetSqlWhere(ht, htWhere, User_MSSQL);
            if (!String.IsNullOrEmpty(sqlWhere) && (sqlWhere.Trim().IndexOf(" ") > 0 || sqlWhere.Length > 4))
            {
                sqlWhere = sqlWhere.Trim();
                if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
            }
            else
            {
                sqlWhere = "  ";
            }
            if (!String.IsNullOrEmpty(排序条件))
            {
                排序条件 = 排序条件.Trim();
                if (!排序条件.StartsWith("order by"))
                {
                    排序条件 = " order by " + 排序条件;
                }
            }
            sqlWhere = "select * from " + DbTable + MSSQL_NOLOCK + " " + sqlWhere + " " + 排序条件;
            return this.ToList(sqlWhere, htWhere);
        }
        /// <summary>
        /// 得到分页后信息
        /// </summary>
        /// <param name="sqlWhere"></param>
        /// <param name="ht"></param>
        /// <param name="返回列"></param>
        /// <param name="排序条件"></param>
        /// <param name="当前页数"></param>
        /// <param name="每页条数"></param>
        /// <returns></returns>
        public List<T> ToList(string sqlWhere, Hashtable ht, string 排序条件, int 当前页数, int 每页条数, string 返回列 = "*")
        {
            return ToList<T>(sqlWhere, ht, 排序条件, 当前页数, 每页条数, 返回列);

        }

        public List<T> ToList(string sql, Hashtable ht = null, string connstr = null)
        {
            return this.ToList<T>(RunDB.GetDataTable(sql, ht, connstr ?? DbConnectionString));
        }


        private string _DbTable = null;

        public string GetDbTableByClassName(Type ty)
        {
            Type t = ty.GetType();
            object[] roles = t.GetCustomAttributes(typeof(SQLSpring), false);
            if (roles != null)
            {
                return (roles[0] as SQLSpring).TableName;
            }
            return ty.Name;
        }
        public string DbTable
        {
            get
            {
                if (!String.IsNullOrEmpty(_DbTable)) return _DbTable;

                _DbTable = Spring.TableName;

                return _DbTable;
            }
        }
        private string _pkname = null;
        public string PkName
        {
            get
            {
                if (!String.IsNullOrEmpty(_pkname)) return _pkname;
                _pkname = Spring.PKName;
                return _pkname;
            }
        }

        private string _MatchCol = null;
        public string MatchColumns
        {
            get
            {
                if (!String.IsNullOrEmpty(_MatchCol)) return _MatchCol;
                _MatchCol = Spring.Columns;
                return _MatchCol;
            }
        }
        private bool IsValidMyType(Type t)
        {
            return (t != null && t.BaseType.Equals(typeof(Article)));
        }
        /// <summary>
        /// 根据PK值获取某个对象
        /// </summary>
        /// <param name="pkvalue"></param>
        /// <returns></returns>
        public T GetByPKValue(object pkvalue)
        {
            if (pkvalue == null) return null;
            Hashtable ht = new Hashtable(1);
            ht[PkName] = pkvalue;
            return GetOne(ht);
        }

        public List<T> GetByPKValues(IEnumerable<object> pkvalue)
        {
            if (pkvalue == null || pkvalue.Count() < 1) return null;
            Hashtable ht = new Hashtable(1);
            ht[PkName + "%in"] = String.Join(",", from ip in pkvalue where ip != null select string.Format("'{0}'", ip.ToString().Replace("'", "")));
            return ToList<T>(ht);
        }
        /// <summary>
        /// 根据主键 返回一个某个异性对象
        /// </summary>
        /// <param name="pkvalue"></param>
        /// <returns></returns>
        public Z GetByPKValue<Z>(object pkvalue)
        {
            if (pkvalue == null) return default(Z);
            Hashtable ht = new Hashtable(1);
            ht[PkName] = pkvalue;
            return GetOne<Z>(ht);
        }
        public int DeleteOne(object pkvalue)
        {
            if (pkvalue == null) return 0;
            Hashtable ht = new Hashtable(1);
            ht[PkName] = pkvalue;
            return DeleteByHashtable(ht);
        }

        public int DeleteByHashtable(Hashtable ht)
        {
            if (ht == null || ht.Count < 1) return -1;//不允许清空一个表
            Hashtable htWhere = new Hashtable();
            string sql = "delete from " + DbTable + "  ";
            string sqlWhere = IOHelper.GetSqlWhere(ht, htWhere, User_MSSQL);
            if (!String.IsNullOrEmpty(sqlWhere) && (sqlWhere.Trim().IndexOf(" ") > 0 || sqlWhere.Length > 4))
            {
                sqlWhere = sqlWhere.Trim();

                if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
                sql += sqlWhere;
            }
            return RunDB.ExecuteNonQuery(sql, htWhere, DbConnectionString);
        }


        #region 小对象注入

        /// <summary>
        /// 得到分页后信息
        /// </summary>
        /// <param name="ht"></param>
        /// <param name="返回列"></param>
        /// <param name="排序条件"></param>
        /// <param name="当前页数"></param>
        /// <param name="每页条数"></param>
        /// <returns></returns>
        public List<Z> ToList<Z>(Hashtable ht, string 排序条件, int 当前页数, int 每页条数, string 返回列 = "*", string fromTable = null)
        {
            Hashtable htWhere = new Hashtable();
            string sqlWhere = IOHelper.GetSqlWhere(ht, htWhere, User_MSSQL);
            if (!String.IsNullOrEmpty(sqlWhere) && (sqlWhere.Trim().IndexOf(" ") > 0 || sqlWhere.Length > 4))
            {
                sqlWhere = sqlWhere.Trim();
                if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
            }
            else
            {
                sqlWhere = "  ";
            }
            return this.ToList<Z>(sqlWhere, htWhere, 排序条件, 当前页数, 每页条数, 返回列, fromTable);
        }

        static DataTable GenericListToDataTable(List<T> list)
        {
            DataTable dt = null;
            Type listType = list.GetType();//IsGenericType
            var sp = Spring;
            string TableName = sp.TableName, PKName = sp.PKName, Columns = sp.Columns;
            var 自增编号 = sp.IdentityPK;
            //determine the underlying type the List<> contains  
            Type elementType = listType.GetGenericArguments()[0];

            //create empty table -- give it a name in case  
            //it needs to be serialized  
            dt = new DataTable(TableName);

            //define the table -- add a column for each public  
            //property or field  
            MemberInfo[] miArray = elementType.GetMembers(
                BindingFlags.Public | BindingFlags.Instance);
            foreach (MemberInfo mi in miArray)
            {
                if (mi.MemberType == MemberTypes.Property)
                {
                    PropertyInfo pi = mi as PropertyInfo;
                    var piName = pi.Name;
                    if (((string.IsNullOrWhiteSpace(Columns) || Columns.Equals("*")) || Columns.IndexOf(piName) != -1) && (!(piName.Equals(PKName) && 自增编号)))
                    {
                        dt.Columns.Add(piName, pi.PropertyType);
                    }

                }
                else if (mi.MemberType == MemberTypes.Field)
                {
                    FieldInfo fi = mi as FieldInfo;
                    var piName = fi.Name;
                    if (((string.IsNullOrWhiteSpace(Columns) || Columns.Equals("*")) || Columns.IndexOf(piName) != -1) && (!(piName.Equals(PKName) && 自增编号)))
                    {
                        dt.Columns.Add(piName, fi.FieldType);
                    }
                }
            }

            foreach (var record in list)
            {
                int i = 0;
                object[] fieldValues = new object[dt.Columns.Count];
                foreach (DataColumn c in dt.Columns)
                {
                    MemberInfo mi = elementType.GetMember(c.ColumnName)[0];
                    if (mi.MemberType == MemberTypes.Property)
                    {
                        PropertyInfo pi = mi as PropertyInfo;
                        fieldValues[i] = pi.GetValue(record, null);
                    }
                    else if (mi.MemberType == MemberTypes.Field)
                    {
                        FieldInfo fi = mi as FieldInfo;
                        fieldValues[i] = fi.GetValue(record);
                    }
                    i++;
                }
                dt.Rows.Add(fieldValues);
            }
            return dt;
        }


        public int BulkInsert(List<T> list, bool usePage = false, int maxPage = 1000)
        {
            if (list == null || list.Count() < 1) return 0;
            int effectCount = 0;
            if (usePage)
            {
                int pagesize = maxPage;
                int page = 1;
                int pageCount =Common.GetPageCount(list.Count(), pagesize);
                for (; page <= pageCount; page++)
                {
                    DataTable dt = GenericListToDataTable(list.Skip((page - 1) * pagesize).Take(pagesize).ToList());
                    effectCount += RunDB.Bulk(dt, dt.TableName, DbConnectionString);
                }
            }
            else
            {
                DataTable dt = GenericListToDataTable(list);
                effectCount = RunDB.Bulk(dt, dt.TableName, DbConnectionString);
            }
            return effectCount;
        }
        public List<Z> ToList<Z>(Hashtable ht, string 排序条件 = null)
        {
            Hashtable htWhere = new Hashtable();
            string sqlWhere = IOHelper.GetSqlWhere(ht, htWhere, User_MSSQL);
            if (!String.IsNullOrEmpty(sqlWhere) && (sqlWhere.Trim().IndexOf(" ") > 0 || sqlWhere.Length > 4))
            {
                sqlWhere = sqlWhere.Trim();
                if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
            }
            else
            {
                sqlWhere = "  ";
            }
            sqlWhere = "select * from " + DbTable + " " + sqlWhere + " " + 排序条件;
            return this.ToList<Z>(sqlWhere, htWhere);
        }
        /// <summary>
        /// 得到分页后信息
        /// </summary>
        /// <param name="sqlWhere"></param>
        /// <param name="ht"></param>
        /// <param name="返回列"></param>
        /// <param name="排序条件"></param>
        /// <param name="当前页数"></param>
        /// <param name="每页条数"></param>
        /// <returns></returns>
        public List<Z> ToList<Z>(string sqlWhere, Hashtable ht, string 排序条件, int 当前页数, int 每页条数, string 返回列 = "*", string fromTable = null)
        {
            string tableName = fromTable ?? (DbTable + MSSQL_NOLOCK);
            当前页数 = Math.Max(1, 当前页数);
            每页条数 = Math.Max(0, 每页条数);
            if (String.IsNullOrEmpty(返回列)) 返回列 = "*";
            if (!String.IsNullOrEmpty(sqlWhere) && (sqlWhere.Trim().IndexOf(" ") > 0 || sqlWhere.Length > 4))
            {
                sqlWhere = sqlWhere.Trim();
                if (!sqlWhere.StartsWith("where ", StringComparison.OrdinalIgnoreCase)) sqlWhere = " where " + sqlWhere;
            }
            else
            {
                sqlWhere = " ";

            }

            string sql = null;
            if (String.IsNullOrWhiteSpace(排序条件))
            {
                排序条件 = "";
            }
            else
            {

                if (!排序条件.ToLower().Trim().Contains("order by"))
                {
                    排序条件 = "order by " + 排序条件;
                }
            }


            if (User_MSSQL)//mssql 2005+
            {
                if (当前页数.Equals(1))
                {
                    sql = "select  top " + 每页条数.ToString() + " " + 返回列 + " from " + tableName + sqlWhere + "  " + 排序条件;
                }
                else
                {
                    string may_排序条件 = 排序条件;
                    string may_Groupby条件 = String.Empty;
                    if (排序条件.Trim().StartsWith("group by "))
                    {
                        int order_by_pos = 排序条件.IndexOf("order by");
                        if (order_by_pos > 0)
                        {
                            may_排序条件 = 排序条件.Substring(order_by_pos);
                            may_Groupby条件 = 排序条件.Substring(0, order_by_pos);
                        }
                    }
                    string topartsql = "select  " + 返回列 + ",(row_number() over ( " + may_排序条件 + ")) r from " + tableName + " " + sqlWhere + " " + may_Groupby条件;
                    sql = String.Format("select * from ( " + topartsql + "  ) t where r between {0} and {1}", (当前页数 - 1) * 每页条数 + 1, 当前页数 * 每页条数);
                }
            }
            else
            {
                sql = "select  " + 返回列 + " from " + tableName + " " + sqlWhere + "  " + 排序条件 + " limit " + ((当前页数 - 1) * 每页条数) + "," + 每页条数.ToString();
            }


            //检查是否在where条件包含group by 条件，逻辑提取并替换
            return this.ToList<Z>(sql, ht);
        }

        public List<Z> ToList<Z>(string sql, Hashtable ht = null, string connstr = null)
        {
            return this.ToList<Z>(RunDB.GetDataTable(sql, ht, connstr ?? DbConnectionString));
        }
        public List<Z> ToList<Z>(DataTable dt)
        {
            if (Common.IsNull(dt)) return null;
            List<Z> ls = new List<Z>();
            foreach (DataRow dr in dt.Rows)
            {
                ls.Add(FlushTo<Z>(dr));
            }
            return ls;
        }

        #endregion
        public static ConcurrentDictionary<string, PropertyInfo[]> _缓存反射 = new ConcurrentDictionary<string, PropertyInfo[]>();
        public Z FlushTo<Z>(DataRow dr)
        {
            Z obj = default(Z);


            if (dr == null)
            {
                return default(Z);
            }
            var cacheKey = typeof(Z).FullName;
            PropertyInfo[] PIS = null;
            if (_缓存反射.ContainsKey(cacheKey))
            {
                PIS = _缓存反射[cacheKey];
            }
            else
            {
                //Type t = obj.GetType();
                PIS = typeof(Z).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (!_缓存反射.ContainsKey(cacheKey))
                {
                    _缓存反射.AddOrUpdate(cacheKey, PIS, (k, v) => { return v; });
                }
            }
            Dictionary<string, bool> setKey = new Dictionary<string, bool>();
            obj = (Z)Activator.CreateInstance(typeof(Z));
            #region 绑定值
            foreach (PropertyInfo pi in PIS)
            {
                if (!pi.CanWrite) continue;

                string piName = pi.Name;
                //object v = pi.GetValue(this, null);
                //dr.Table.Columns.Contains(name) 或者Regex.IsMatch(piName, "^" + Columns + "$", RegexOptions.IgnoreCase | RegexOptions.Compiled)
                if (dr.Table.Columns.Contains(piName) && !dr[piName].Equals(System.DBNull.Value))
                {
                    if (!setKey.ContainsKey(piName))
                    {
                        setKey.Add(piName, true);
                    }

                    if (pi.PropertyType.IsEnum)
                    {
                        pi.SetValue(obj, Enum.ToObject(pi.PropertyType, dr[piName]), null);
                    }
                    else if (pi.PropertyType.Equals(typeof(int)) && !dr[piName].GetType().Equals(typeof(int)))
                    {
                        int dv = 0;
                        var rawv = dr[piName].ToString();
                        if (int.TryParse(rawv, out dv))
                        {
                            pi.SetValue(obj, dv, null);
                        }
                        else if ("True" == rawv || "true" == rawv || "是" == rawv)
                        {
                            pi.SetValue(obj, 1, null);
                        }
                    }
                    else if (dr[piName].GetType().Equals(typeof(System.Guid)) && pi.PropertyType.Equals(typeof(string)))
                    {
                        pi.SetValue(obj, dr[piName].ToString(), null);
                    }
                    else if (dr[piName].GetType().Equals(typeof(System.DateTime)) && pi.PropertyType.Equals(typeof(string)))
                    {
                        pi.SetValue(obj, dr[piName].ToString(), null);//yyyy-MM-dd HH:mm:ss
                    }
                    else if ((!dr[piName].GetType().Equals(typeof(System.String))) && pi.PropertyType.Equals(typeof(string)))
                    {
                        pi.SetValue(obj, dr[piName].ToString(), null);
                    }
                    else if ((dr[piName].GetType().Equals(typeof(System.Boolean))) && pi.PropertyType.Equals(typeof(int)))
                    {
                        pi.SetValue(obj, ((bool)dr[piName]) ? 1 : 0, null);
                    }
                    else if ((dr[piName].GetType().Equals(typeof(System.UInt64))) && pi.PropertyType.Equals(typeof(System.Int64)))
                    {
                        pi.SetValue(obj, long.Parse(dr[piName].ToString()), null);
                    }
                    else if (pi.PropertyType.Equals(typeof(Dictionary<string, string>)))
                    {
                        pi.SetValue(obj, JSONHelper.FromJSON<Dictionary<string, string>>(dr[piName].ToString()), null);
                    }
                    else if (!pi.PropertyType.IsValueType && pi.PropertyType != typeof(string))
                    {
                        var tos =JSONHelper.FromJSON(dr[piName].ToString(), pi.PropertyType);
                        pi.SetValue(obj, tos, null);
                    }
                    else
                    {

                        pi.SetValue(obj, dr[piName], null);
                    }
                }
            }
            #endregion
            var skipColumns = from xi in PIS where xi.PropertyType.Equals(typeof(Dictionary<string, string>)) select xi;

            PropertyInfo 扩展字段 = null;
            foreach (var xi in skipColumns)
            {
                if (setKey.ContainsKey(xi.Name)) continue;
                if (xi.Name.Contains("ExtVals"))
                {
                    扩展字段 = xi;
                    break;
                }
            }
            if (扩展字段 != null)
            {
                Dictionary<string, string> LastSkips = new Dictionary<string, string>();
                foreach (DataColumn c in dr.Table.Columns)
                {
                    if (setKey.ContainsKey(c.ColumnName)) continue;
                    if (MatchColumns.Contains("|" + c.ColumnName + "|")) continue;
                    var ko = dr[c.ColumnName].ToString();
                    if (!string.IsNullOrEmpty(ko))
                    {
                        LastSkips.Add(c.ColumnName, ko);
                    }
                }
                扩展字段.SetValue(obj, LastSkips, null);
            }


            return obj;
        }
        public bool HasId(string id)
        {
            var htq = new Hashtable(1);
            htq[PkName] = id;
            return GetCount(htq) > 0;
        }
        static string GetRandString(int len)
        {
            string preSafe = null;
            for (int i = 0; i < len; i++)
            {
                var prestr = Common.GetRandomNumber(0, 61);
                if (prestr < 10)
                {
                    preSafe += (char)(prestr + 48);
                }
                else if (prestr < 36)
                {
                    preSafe += (char)(prestr - 10 + 65);
                }
                else
                {
                    preSafe += (char)(prestr - 36 + 97);
                }
            }
            return preSafe;
        }
        public string GetSafeGuid(bool checkDb = false, int len = 15)
        {
            bool findStr = false;
            string preSafe = GetRandString(len);
            if (checkDb)
            {

                int loopCount = 0;
                do
                {
                    findStr = HasId(preSafe);
                    if (!findStr) break;
                    preSafe = GetRandString(len);
                } while (findStr && loopCount < 1000);
            }
            return preSafe;
        }


        #region 初始化
        private static Dictionary<string, string> _p_缓存的数据库列 = new Dictionary<string, string>();
        public string __缓存的数据库列
        {
            get
            {
                if (_p_缓存的数据库列.ContainsKey(DbTable)) return _p_缓存的数据库列[DbTable];
                return AutoLoadColumns();
            }
        }
        private static object olock = new object();
        private string AutoLoadColumns()
        {
            if (_p_缓存的数据库列.ContainsKey(DbTable)) return _p_缓存的数据库列[DbTable];
            var rawto = string.Empty;
            lock (olock)
            {
                var __p = new Dictionary<string, bool>();
                System.Data.DataTable colsTable = null;
                string fetchSql = string.Format("select * from {0} where 0", DbTable);
                colsTable = RunDB.GetDataTable(fetchSql, null, DbConnectionString);

                if (colsTable == null || colsTable.Columns == null || colsTable.Columns.Count < 1)
                {
                    throw new Exception("数据表读取失败");
                }
                foreach (System.Data.DataColumn xi in colsTable.Columns)
                {
                    if (xi.ColumnName.Equals(PkName, StringComparison.CurrentCultureIgnoreCase)) continue;
                    if (!__p.ContainsKey(xi.ColumnName))
                    {
                        __p.Add(xi.ColumnName, true);
                    }
                }
                rawto = (string.Join("|", __p.Keys).GetForkColumn());
                _p_缓存的数据库列.Add(DbTable, rawto);//.ToLower()

            }
            return rawto;
        }
        #endregion

    }

}
