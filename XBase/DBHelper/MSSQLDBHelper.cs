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
    public class MSSQLDBHelper:AbsDBHelper
    {
        public override string GetAutoCols(string id)
        {
            return string.Format("select  SCOPE_IDENTITY() as {0}", id);
        }

        public override int Execute(string sQuery, Hashtable hashParms=null, string sDbConn=null)
        {
            return _MSSql.Execute(sDbConn, sQuery, hashParms);
        }
        public override int ExecuteNonQuery(string sQuery,Hashtable hashParms = null, string sDbConn = null)
        {
            return _MSSql.ExecuteNonQuery(sQuery, hashParms, sDbConn);
        }
        public override object GetObject(string sQuery, Hashtable hashParms = null, string sDbConn = null)
        {
            return _MSSql.GetObject(sQuery, hashParms, sDbConn);
        }


        public override System.Data.DataSet GetDataSet(string sQuery, Hashtable hashParms = null, string sDbConn = null)
        {
            return _MSSql.GetDataSet(sQuery, hashParms, sDbConn);
        }

        public override int DoTrans(ArrayList aQuery, ArrayList aPara,  string sDbConn = null)
        {
            return _MSSql.DoTrans(sDbConn, aQuery, aPara);
        }
        public override int Bulk(DataTable dt, string tableName, string sDbConn = null)
        {
            return _MSSql.Bulk(dt, tableName,5000, sDbConn);
        }
    }
}
