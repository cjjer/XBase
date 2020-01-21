namespace XBase.SqlHelper
{
    using System.Data;
    using System.Collections;
    using System;
    using System.IO;
    using MySql.Data.MySqlClient;
    using System.Text;
    using System.Collections.Generic;
    using System.Linq;
    public class _MySql
    {
        public static string _DbConnString = System.Configuration.ConfigurationManager.AppSettings["DbConnString"];
        public static bool NoThrowError = "0".Equals(System.Configuration.ConfigurationManager.AppSettings["ThrowError"]);
        public static int CommandTimeout = int.Parse(System.Configuration.ConfigurationManager.AppSettings["CommandTimeout"] ?? "120");
        #region Log
        private static object olockfile = new object();

        #endregion


        public static int Bulk(DataTable dataTable, string tableName, string sDbConn = null)
        {
            if (dataTable == null || dataTable.Rows.Count < 1) return 0;
            List<string> cols = new List<string>();
            if (dataTable.Columns.Count < 1)
            {
                return 0;
            }
            foreach (DataColumn dc in dataTable.Columns)
            {
                cols.Add("`" + (dc.ColumnName) + "`");//
            }
            ;
            StringBuilder sCommand = new StringBuilder("INSERT INTO `" + tableName + "` (" + string.Join(",", cols) + ") VALUES ");
            using (MySqlConnection mConnection = new MySqlConnection(sDbConn ?? _DbConnString))
            {
                int eachHit = 0;
                int maxHit = dataTable.Rows.Count;
                foreach (DataRow dr in dataTable.Rows)
                {
                    eachHit++;
                    List<string> Rows = new List<string>();
                    foreach (DataColumn dc in dataTable.Columns)
                    {
                        Rows.Add(string.Format("'{0}'", MySqlHelper.EscapeString(dr[dc.ColumnName].ToString())));
                    }
                    sCommand.Append("(" + string.Join(",", Rows) + ")");
                    if (!eachHit.Equals(maxHit))
                    {
                        sCommand.Append(",");
                    }
                }
                sCommand.Append(";");
                mConnection.Open();
                using (MySqlCommand myCmd = new MySqlCommand(sCommand.ToString(), mConnection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }
            }
            return dataTable.Rows.Count;
        }

        public static DataSet GetDataSet(string sQuery, Hashtable hashParas = null, string sDbConn = null)
        {
            return GetDataSetMain(sQuery, ConvertHashTabletoSqlParameterArray(hashParas), sDbConn ?? _DbConnString);

        }
        public static int Execute(string sQuery, Hashtable hashParms, string sDbConn = null)
        {
            return ExecuteMain(sQuery, ConvertHashTabletoSqlParameterArray(hashParms), sDbConn);
        }
        public static object GetObject(string sQuery, Hashtable hashParms, string sDbConn = null)
        {
            return GetObjectMain(sQuery, ConvertHashTabletoSqlParameterArray(hashParms), sDbConn);
        }
        private static object GetObjectMain(string sQuery, MySqlParameter[] sqlParms, string sDbConn)
        {
            object obj = null;
            using (var objConn = new MySqlConnection(sDbConn ?? _DbConnString))
            {
                try
                {
                    objConn.Open();
                    MySqlCommand objCmd = new MySqlCommand("", objConn);
                    PrepareCommand(objCmd, sQuery, sqlParms);
                    obj = objCmd.ExecuteScalar();

                }
                catch (System.Exception e)
                {
                    SetErrorTrace("GetObjectWithPara	" + sQuery + "	" + e.Message);
                    if (!NoThrowError)
                        throw (e);
                }
                return obj;
            }
        }


        private static DataSet GetDataSetMain(string sQuery, MySqlParameter[] sqlParms, string sDbConn)
        {

            DataSet ds = new DataSet();
            using (var objConn = new MySqlConnection(sDbConn ?? _DbConnString))
            {

                try
                {
                    using (MySqlCommand objCmd = new MySqlCommand("", objConn))
                    {
                        PrepareCommand(objCmd, sQuery, sqlParms);
                        using (var da = new MySqlDataAdapter(objCmd))
                        {
                            da.Fill(ds);
                        }
                    }
                }
                catch (System.Exception e)
                {

                    SetErrorTrace("GetDataSetwithPara	" + sQuery + "	" + e.Message);
                    //	SetErrorTrace(sQuery,e.Message);
                    if (!NoThrowError)
                        throw (e);
                }
                return ds;
            }

        }
        public static void SetErrorTrace(string sMessage)
        {
            try
            {
                string sDateTag = System.DateTime.Now.ToString("yyyyMMdd");

                string sPath = System.Configuration.ConfigurationManager.AppSettings["LogPath"];
                if (String.IsNullOrWhiteSpace(sPath)) sPath = AppDomain.CurrentDomain.BaseDirectory;

                sPath = Path.Combine(sPath.Replace("//", "/"), "wronglog/" + sDateTag);
                if (!Directory.Exists(sPath)) Directory.CreateDirectory(sPath);

                string sFile = sPath + "/wrong.config";
                lock (olockfile)
                {
                    using (StreamWriter sw = new StreamWriter(sFile, true))
                    {
                        sw.WriteLine(sMessage + " " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        sw.Flush();
                        sw.Close();
                    }

                }
            }
            catch (Exception exp)
            {
                throw new Exception("写sql日志:" + exp.ToString());
            }

        }

        public static int ExecuteNonQuery(string sqlstr, Hashtable ht, string connstr = null)
        {
            return DoTrans(sqlstr, ConvertHashTabletoSqlParameterArray(ht), connstr ?? _DbConnString);
        }

        public static int DoTrans(string sQuery, MySqlParameter[] sqlParms, string sDbConn)
        {
            ArrayList arayQuery = new ArrayList();
            arayQuery.Add(sQuery);
            ArrayList arayPara = null;
            if (sqlParms != null)
            {
                arayPara = new ArrayList();
                arayPara.Add(sqlParms);
            }
            int[] ls = DoTransMain(sDbConn, arayQuery, arayPara);
            if (ls == null || ls.Length < 1) return 0;
            return ls[0];

        }

        private static int ExecuteMain(string sQuery, MySqlParameter[] sqlParms, string sDbConn)
        {
            using (var objConn = new MySqlConnection(sDbConn ?? _DbConnString))
            {
                try
                {
                    objConn.Open();
                    using (MySqlCommand objCmd = new MySqlCommand("", objConn))
                    {
                        PrepareCommand(objCmd, sQuery, sqlParms);
                        return objCmd.ExecuteNonQuery();
                    }
                }
                catch (System.Exception e)
                {
                    SetErrorTrace("ExcecuteWithPara	" + sQuery + "	" + e.Message);
                    if (!NoThrowError)
                        throw (e);
                }
                objConn.Close();
            }
            return -1;
        }
        public static int DoTrans(string sDbConn, ArrayList aQuery, ArrayList aPara)
        {
            int[] ls = DoTransMain(sDbConn, aQuery, aPara);
            if (ls == null || ls.Length < 1) return 0;
            return ls[0];
        }
        public static int[] DoTransMain(string sDbConn, ArrayList aQuery, ArrayList aPara)
        {
            if (aQuery == null || aQuery.Count < 1) return (new int[] { -1 });
            int[] Al = new int[aQuery.Count];
            using (var objConn = new MySqlConnection(sDbConn ?? _DbConnString))
            {
                objConn.Open();
                MySqlCommand objCmd = new MySqlCommand();
                objCmd.Connection = objConn;
                MySqlTransaction myTrans = null;

                try
                {
                    myTrans = objConn.BeginTransaction();
                    objCmd.Transaction = myTrans;
                    if (aPara == null || aPara.Count == 0)
                    {
                        foreach (object obj in aQuery)
                        {
                            PrepareCommand(objCmd, (string)obj, null);
                            objCmd.ExecuteNonQuery();
                        }

                    }
                    else if (aQuery.Count <= aPara.Count)
                    {
                        object objItem = aPara[0];
                        System.Type typeItem = objItem.GetType();
                        if (typeItem == typeof(Hashtable))
                            for (int i = 0; i < aQuery.Count; i++)
                            {
                                PrepareCommand(objCmd, aQuery[i].ToString(), ConvertHashTabletoSqlParameterArray((Hashtable)aPara[i]));
                                Al[i] = objCmd.ExecuteNonQuery();
                            }
                        else if (typeItem == typeof(MySqlParameter[]))
                            for (int i = 0; i < aQuery.Count; i++)
                            {
                                PrepareCommand(objCmd, (string)aQuery[i], (MySqlParameter[])aPara[i]);
                                Al[i] = objCmd.ExecuteNonQuery();
                            }

                        else
                            throw new Exception("ArrayList包含不是Hashtable 或 SqlParameter数组");
                    }

                    myTrans.Commit();

                }	//end try			
                catch (System.Exception e)
                {
                    try
                    {
                        myTrans.Rollback();
                    }
                    catch (Exception e1)
                    {
                        SetErrorTrace(String.Format("事务回滚失败{0}	{1}", objCmd.CommandText, e1.ToString()));
                    }
                    SetErrorTrace(String.Format("事务更新失败{0}	{1}", objCmd.CommandText, e.ToString()));
                    if (!NoThrowError)
                        throw (e);

                }
                finally
                {

                    objConn.Close();
                }
            }
            return Al;
        }

        private static MySqlParameter[] ConvertHashTabletoSqlParameterArray(Hashtable hashTable)
        {
            if (hashTable == null)
                return null;
            int count = hashTable.Count;
            MySqlParameter[] sqlParams = new MySqlParameter[count];
            int i = 0;
            foreach (DictionaryEntry hashItem in hashTable)
            {

                //if (hashItem.Value!=null&&hashItem.Value.GetType() == typeof(string))
                //{
                //    MySql.Data.MySqlClient.MySqlHelper.EscapeString(hashItem.Value.ToString());
                //}
                sqlParams[i] = new MySqlParameter("@" + hashItem.Key.ToString().TrimStart('@'), hashItem.Value);
                i++;
            }
            return sqlParams;

        }
        private static void PrepareCommand(MySqlCommand objCmd, string sQuery, MySqlParameter[] sqlParms)
        {
            PrepareCommand(objCmd, sQuery, sqlParms, CommandType.Text);

        }
        private static void PrepareCommand(MySqlCommand objCmd, string sQuery, MySqlParameter[] sqlParms, CommandType cmdType)
        {
            objCmd.Parameters.Clear();
            objCmd.CommandText = sQuery;
            objCmd.CommandType = cmdType;//cmdType;
            objCmd.CommandTimeout = CommandTimeout;


            if (sqlParms != null)
            {
                foreach (var sqlPara in sqlParms)
                {
                    objCmd.Parameters.Add(sqlPara);
                }
            }

        }


    };

}