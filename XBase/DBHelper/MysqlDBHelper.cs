using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XBase.SqlHelper;
using System.Collections;
using System.Data;

namespace XBase.DBHelper
{
    public class MySQLDBHelper:AbsDBHelper
    {
        public override string GetAutoCols(string id)
        {
            return string.Format("select  LAST_INSERT_ID() as {0}", id);
        }
        public override int Execute(string sQuery, Hashtable hashParms = null, string sDbConn = null)
        {
            return _MySql.Execute(sQuery, hashParms, sDbConn);
        }
        public override int ExecuteNonQuery(string sQuery, Hashtable hashParms = null, string sDbConn = null)
        {
            return _MySql.ExecuteNonQuery(sQuery, hashParms, sDbConn);
        }
        public override object GetObject(string sQuery, Hashtable hashParms = null, string sDbConn = null)
        {
            return _MySql.GetObject(sQuery, hashParms, sDbConn);
        }


        public override System.Data.DataSet GetDataSet(string sQuery, Hashtable hashParms = null, string sDbConn = null)
        {
            return _MySql.GetDataSet(sQuery, hashParms, sDbConn);
        }

        public override int DoTrans(ArrayList aQuery, ArrayList aPara, string sDbConn = null)
        {
            return _MySql.DoTrans(sDbConn, aQuery, aPara);
        }


        public override int Bulk(DataTable dt, string tableName, string sDbConn = null)
        {
            return _MySql.Bulk(dt, tableName, sDbConn);
        }
    }
}
