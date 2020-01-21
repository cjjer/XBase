using System;
using System.Collections.Generic;
using System.Text;

namespace XBase.DAO
{
    [AttributeUsage(AttributeTargets.Class , AllowMultiple = false, Inherited = true)]
    public class SQLSpring : Attribute
    {
        private string _TableName, _PKName;
        private string _Columns = null;
        private bool _IdentityPK = true;
        //private DbLogHanlder _DbLogHanlder = (DbLogHanlder.Addnew | DbLogHanlder.Update | DbLogHanlder.Delete);

        public SQLSpring()
        {
        }
        public SQLSpring(string tablename, string pkname)
            : this(tablename, pkname, null)
        {

        }
        public SQLSpring(string tablename, string pkname, string cols)
            : this(tablename, pkname, cols, true)
        {
        }
        public SQLSpring(string tablename, string pkname, string cols, bool identityPK)
        {
        
            this._TableName = tablename;
            this._PKName = pkname;
            this._Columns = cols;
            this._IdentityPK = identityPK;
        }
        /// <summary>
        /// 与数据库映射到字段，符号条件的正则表达式 ,与 附加顺序有关。
        /// </summary>
        public string Columns { get { return _Columns; } set { _Columns = value; } }
        public string TableName { get { return _TableName; } }
        public string PKName { get { return _PKName; } }
        public bool IdentityPK { get { return _IdentityPK; } set { _IdentityPK = value; } }
        public override string ToString()
        {
            return String.Format("对应的表名是{0}，主键列名是{1} ,{2}", TableName, PKName, Columns);
        }
    }
    //public enum DbLogHanlder
    //{
    //    None=16,Addnew = 1, Update = 2, Delete = 4, Search = 8
    //}
}
