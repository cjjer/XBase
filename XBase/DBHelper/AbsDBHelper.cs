using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XBase.Utility;

namespace XBase.DBHelper
{
    public abstract class AbsDBHelper
    {
        public abstract string GetAutoCols(string id);
        public abstract int Bulk(DataTable dts, string tableName, string sDbConn = null);

        public abstract int DoTrans(ArrayList aQuery, ArrayList aPara, string sDbConn = null);

        /// <summary>
        /// 执行写操作
        /// </summary>
        /// <param name="sQuery"></param>
        /// <param name="hashParms"></param>
        /// <param name="sDbConn"></param>
        /// <returns></returns>
        public abstract int ExecuteNonQuery(string sQuery, Hashtable hashParms = null, string sDbConn = null);
        /// <summary>
        /// 执行读操作
        /// </summary>
        /// <param name="sQuery"></param>
        /// <param name="hashParms"></param>
        /// <param name="sDbConn"></param>
        /// <returns></returns>
        public abstract int Execute(string sQuery, Hashtable hashParms = null, string sDbConn = null);
        /// <summary>
        /// 汇总获取数量
        /// </summary>
        /// <param name="sQuery"></param>
        /// <param name="hashParms"></param>
        /// <param name="sDbConn"></param>
        /// <returns></returns>
        public abstract object GetObject(string sQuery, Hashtable hashParms = null, string sDbConn = null);

        public  DataRow GetRow(string sQuery, Hashtable hashParms = null, string sDbConn = null)
        {
            var sets = GetDataTable(sQuery, hashParms, sDbConn);
            if (sets == null || sets.Rows.Count<1) return null;
            return sets.Rows[0];
        }

        public  DataTable GetDataTable(string sQuery, Hashtable hashParms = null, string sDbConn = null)
        {
            var sets = GetDataSet(sQuery, hashParms, sDbConn);
            if (sets == null || sets.Tables.Count < 1) return null;
            return sets.Tables[0];
        }
        public abstract DataSet GetDataSet(string sQuery, Hashtable hashParms = null, string sDbConn = null);
        public int GetCount(string sQuery, Hashtable hashParms, string sDbConn = null)
        {
            var d = GetObject(sQuery, hashParms, sDbConn);
            if (d == null) return 0;
            return Common.CInt(d);
        }

    }
}
