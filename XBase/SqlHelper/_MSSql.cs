namespace XBase.SqlHelper
{
    using System.Data;
    using System.Collections;
    using System.Data.SqlClient;
    using System;
    using System.IO;
    public  class _MSSql 
    {
        public static string _DbConnString = System.Configuration.ConfigurationManager.AppSettings["DbConnString"];
        public static bool NoThrowError = "0".Equals(System.Configuration.ConfigurationManager.AppSettings["ThrowError"]);
        public static int CommandTimeout = int.Parse(System.Configuration.ConfigurationManager.AppSettings["CommandTimeout"] ?? "120");

        #region GetDataSet
        public  static DataSet GetDataSet(string sQuery, string sDbConn)
        {
            if (sDbConn == null)
            {
                sDbConn = _DbConnString;
            }
            SqlParameter[] sParas = null;
            return GetDataSet(sQuery, sParas, sDbConn);

        }
        public static DataSet GetDataSet(string sQuery)
        {
            return GetDataSet(sQuery, _DbConnString);

        }
        public static DataSet GetDataSet(string sQuery, SqlParameter[] sqlParms, string sDbConn)
        {
            return GetDataSetMain(sQuery, sqlParms, sDbConn);
        }
        public static DataSet GetDataSet(string sQuery, Hashtable hashParas, string sDbConn)
        {
            return GetDataSet(sQuery, ConvertHashTabletoSqlParameterArray(hashParas), sDbConn);

        }
        public static DataSet GetDataSet(string sQuery, SqlParameter[] sqlParms)
        {
            return GetDataSet(sQuery, sqlParms, _DbConnString);

        }
        public static DataSet GetDataSet(string sQuery, Hashtable hashParas)
        {

            return GetDataSet(sQuery, hashParas, _DbConnString);

        }
        public static DataRow GetSingleDataRow(string sQuery, Hashtable ht)
        {
            return GetSingleDataRow(sQuery, ht, null); 
        }
        public static DataRow GetSingleDataRow(string sQuery, Hashtable ht,string conn)
        {
            DataSet ds = GetDataSet(sQuery, ht, conn);
            if(ds==null || ds.Tables.Count<1 || ds.Tables[0].Rows.Count<1)return null;
            return ds.Tables[0].Rows[0];

        }
        public static DataSet GetDataSetStoreDb(string sQuery, Hashtable hashParas)
        {
            DataSet ds = new DataSet();
            using (SqlConnection objConn = new SqlConnection(_DbConnString))
            {

                try
                {
                    SqlCommand objCmd = new SqlCommand(sQuery, objConn);
                    PrepareCommand(objCmd, sQuery, ConvertHashTabletoSqlParameterArray(hashParas), CommandType.StoredProcedure);
                    SqlDataAdapter da = new SqlDataAdapter(objCmd);
                    da.Fill(ds);

                }
                catch (System.Exception e)
                {

                    SetErrorTrace("GetDataSetStoreDb	" + sQuery + "	" + e.Message);
                    //	SetErrorTrace(sQuery,e.Message);
                    if (!NoThrowError)
                    {
                        throw (e);
                    }
                }
            }
            return ds;

        }

        private static DataSet GetDataSetMain(string sQuery, SqlParameter[] sqlParms, string sDbConn)
        {

            DataSet ds = new DataSet();
            using (SqlConnection objConn = new SqlConnection(sDbConn??_DbConnString))
            {

                try
                {
                    using (SqlCommand objCmd = new SqlCommand("", objConn))
                    {
                        PrepareCommand(objCmd, sQuery, sqlParms);
                        using (SqlDataAdapter da = new SqlDataAdapter(objCmd))
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
        #endregion

        #region DoTrans
        public static int DoTrans(string sQuery)
        {
            return DoTrans(sQuery, null);

        }


        public static int DoTrans(string sQuery, Hashtable htParas)
        {
            return DoTrans(_DbConnString, sQuery, ConvertHashTabletoSqlParameterArray(htParas));
        }

        public static int DoTrans(string sDbConn, string sQuery, Hashtable hashParms)
        {

            return DoTrans(sDbConn, sQuery, ConvertHashTabletoSqlParameterArray(hashParms));
        }
        public static int DoTrans(string sDbConn, string sQuery, SqlParameter[] sqlParms)
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
        public static int Bulk(DataTable dataTable, string tableName,int batchSize=5000, string sDbConn = null)
        {
            if (dataTable == null || dataTable.Rows.Count<1) return 0;
            using (SqlConnection objConn = new SqlConnection(sDbConn ?? _DbConnString))
            {
                try
                {
                    objConn.Open();

                    using (var bulk = new SqlBulkCopy(objConn, SqlBulkCopyOptions.KeepIdentity, null)
                    {
                        DestinationTableName = tableName,
                        BatchSize = batchSize
                    })
                    {
                        //循环所有列，为bulk添加映射
                        foreach (DataColumn c in dataTable.Columns)
                        {
                            bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);
                        }
                        bulk.WriteToServer(dataTable);
                        bulk.Close();
                    }
                }
                catch (System.Exception e)
                {
                    SetErrorTrace("DotransWithPara (Commit Trans Error)	批量失败	" + e.Message);
                    if (!NoThrowError)
                        throw (e);
                }
                objConn.Close();
            }
            return dataTable.Rows.Count;
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
            using (SqlConnection objConn = new SqlConnection(sDbConn ?? _DbConnString))
            {
				objConn.Open(); 
                SqlTransaction myTrans = null;
                SqlCommand objCmd = null;
                try
                {
                    myTrans = objConn.BeginTransaction();
                    objCmd = new SqlCommand();
                    objCmd.Connection = objConn;
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
                        else if (typeItem == typeof(SqlParameter[]))
                            for (int i = 0; i < aQuery.Count; i++)
                            {
                                PrepareCommand(objCmd, (string)aQuery[i], (SqlParameter[])aPara[i]);
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
                        SetErrorTrace(String.Format("事务回滚失败{0}	{1}",objCmd.CommandText, e1.ToString()));

                    }
                        SetErrorTrace(String.Format("事务更新失败{0}	{1}",objCmd.CommandText, e.ToString()));
                        if (!NoThrowError)
                    throw (e);

                }finally{
                    
					objConn.Close();
                }
            }
            return Al;
        }

        #endregion
        #region DoTransWithRStoredProcedure
        public static void DoTransWithStoredProcedure(string sQuery, Hashtable hashParms)
        {
            DoTransWithStoredProcedure(sQuery, ConvertHashTabletoSqlParameterArray(hashParms));
        }
        public static void DoTransWithStoredProcedure(string sQuery, SqlParameter[] sqlParms)
        {
            DoTransWithStoredProcedure(_DbConnString, sQuery, sqlParms);

        }
        public static void DoTransWithStoredProcedure(string sDbConn, string sQuery, Hashtable hashParms)
        {

            DoTransWithStoredProcedure(sDbConn, sQuery, ConvertHashTabletoSqlParameterArray(hashParms));


        }
        public static void DoTransWithStoredProcedure(string sDbConn, string sQuery, SqlParameter[] sqlParms)
        {
            using (SqlConnection objConn = new SqlConnection(sDbConn ?? _DbConnString))
            {
                SqlTransaction myTrans = null;
                SqlCommand objCmd = null;
                try
                {
                    objConn.Open();
                    myTrans = objConn.BeginTransaction();
                    objCmd = new SqlCommand("", objConn);
                    objCmd.Transaction = myTrans;
                    PrepareCommand(objCmd, sQuery, sqlParms);
                    objCmd.CommandType = CommandType.StoredProcedure;
                    objCmd.ExecuteNonQuery();
                    myTrans.Commit();

                }
                catch (System.Exception e)
                {
                    try
                    {
                        myTrans.Rollback();

                    }
                    catch (Exception e1)
                    {
                        SetErrorTrace("DotransWithParaList(Rollback Trans Error)	" + objCmd.CommandText + "	" + e1.Message);

                    }
                    SetErrorTrace("DotransWithPara (Commit Trans Error)	" + sQuery + "	" + e.Message);
                    if (!NoThrowError)
                    throw (e);
                }

            }


        }
        #endregion
        #region DoTransWithRead
        public static string DoTransWithRead(string sQuery, string sDbConn)
        {
            //	string sDbConn=ConfigurationSettings.AppSettings["SQLConnString1"];
            string sRslt = "";
            SqlConnection objConn = new SqlConnection(sDbConn ?? _DbConnString);
            try
            {
                objConn.Open();
                SqlTransaction myTrans = objConn.BeginTransaction();
                SqlCommand objCmd = new SqlCommand(sQuery, objConn);
                objCmd.Transaction = myTrans;
                try
                {
                    SqlDataReader dr = objCmd.ExecuteReader();
                    if (dr.Read())
                    {
                        sRslt = dr[0].ToString();
                    }
                    else
                        sRslt = "";
                    dr.Close();
                    myTrans.Commit();
                    objConn.Close();

                }
                catch (System.Exception e)
                {
                    try
                    {
                        myTrans.Rollback();

                    }
                    finally
                    {
                        objConn.Close();
                    }
                    SetErrorTrace("DotransWithReadNoPara	" + sQuery + "	" + e.Message);
                    if (!NoThrowError)
                    throw (e);
                }

            }
            catch (System.Exception e)
            {
                objConn.Close();

                throw (e);

            }

            return sRslt;

        }
        public static string DoTransWithRead(string sQuery)
        {
            return DoTransWithRead(sQuery, _DbConnString);



        }
        public static string DoTransWithRead(string sDbConn, string sQuery, SqlParameter[] sqlPara)
        {
            //	string sDbConn=ConfigurationSettings.AppSettings["SQLConnString1"];
            string sRslt = "";
            SqlConnection objConn = new SqlConnection(sDbConn ?? _DbConnString);
            try
            {
                objConn.Open();
                SqlTransaction myTrans = objConn.BeginTransaction();
                SqlCommand objCmd = new SqlCommand("", objConn);
                PrepareCommand(objCmd, sQuery, sqlPara);
                objCmd.Transaction = myTrans;
                try
                {
                    SqlDataReader dr = objCmd.ExecuteReader();
                    if (dr.Read())
                    {
                        sRslt = dr[0].ToString();
                    }
                    else
                        sRslt = "";
                    dr.Close();
                    myTrans.Commit();
                    objConn.Close();

                }
                catch (System.Exception e)
                {
                    try
                    {
                        myTrans.Rollback();

                    }
                    finally
                    {
                        objConn.Close();
                    }
                    SetErrorTrace("DotransWithReadWithPara	" + sQuery + "	" + e.Message);
                    if (!NoThrowError)
                    throw (e);
                }

            }
            catch (System.Exception e)
            {
                objConn.Close();

                throw (e);

            }

            return sRslt;

        }
        public static string DoTransWithRead(string sQuery, SqlParameter[] sqlPara)
        {
            //	string sDbConn=ConfigurationSettings.AppSettings["SQLConnString1"];

            try
            {
                return DoTransWithRead(_DbConnString, sQuery, sqlPara);

            }
            catch (System.Exception e)
            {

                throw (e);

            }


        }
        public static DataRow DoTransWithDatarowRead(string sQuery, string sDbConn)
        {
            //	string sDbConn=ConfigurationSettings.AppSettings["SQLConnString1"];

            SqlConnection objConn = new SqlConnection(sDbConn ?? _DbConnString);
            try
            {
                DataSet ds = new DataSet();
                objConn.Open();
                SqlTransaction myTrans = objConn.BeginTransaction();
                SqlCommand objCmd = new SqlCommand(sQuery, objConn);
                objCmd.Transaction = myTrans;
                SqlDataAdapter da = new SqlDataAdapter(objCmd);
                try
                {
                    da.Fill(ds);


                    myTrans.Commit();
                    objConn.Close();
                    if (ds.Tables.Count > 0)
                    {
                        if (ds.Tables[0].Rows.Count > 0)
                            return ds.Tables[0].Rows[0];
                        else
                            return null;

                    }
                    else
                        return null;


                }
                catch (System.Exception e)
                {
                    try
                    {
                        myTrans.Rollback();

                    }
                    finally
                    {
                        objConn.Close();
                    }
                    SetErrorTrace("DotransWithReadNoPara	" + sQuery + "	" + e.Message);
                    if (!NoThrowError)
                    throw (e);
                }

            }
            catch (System.Exception e)
            {
                objConn.Close();

                throw (e);

            }

            return null;

        }
        public static DataRow DoTransWithDatarowRead(string sQuery)
        {
            //	string sDbConn=ConfigurationSettings.AppSettings["SQLConnString1"];

            try
            {
                return DoTransWithDatarowRead(sQuery, _DbConnString);

            }
            catch (System.Exception e)
            {

                throw (e);

            }


        }
        /*
        public static DataRow DoTransWithDatarowRead(string sDbConn, string sQuery, SqlParameter[] sqlPara)
        {
            //	string sDbConn=ConfigurationSettings.AppSettings["SQLConnString1"];

            SqlConnection objConn = new SqlConnection(sDbConn);
            try
            {
                DataSet ds = new DataSet();
                objConn.Open();
                SqlTransaction myTrans = objConn.BeginTransaction();
                SqlCommand objCmd = new SqlCommand("", objConn);
                PrepareCommand(objCmd, sQuery, sqlPara);
                objCmd.Transaction = myTrans;
                SqlDataAdapter da = new SqlDataAdapter(objCmd);
                try
                {
                    da.Fill(ds);


                    objConn.Close();
                    if (ds.Tables.Count > 0)
                    {
                        if (ds.Tables[0].Rows.Count > 0)
                            return ds.Tables[0].Rows[0];
                        else
                            return null;

                    }
                    else
                        return null;

                }
                catch (System.Exception e)
                {
                    try
                    {
                        myTrans.Rollback();

                    }
                    finally
                    {
                        objConn.Close();
                    }
                    SetErrorTrace("DotransWithReadWithPara	" + sQuery + "	" + e.Message);
                    if (!NoThrowError)
                    throw (e);
                }

            }
            catch (System.Exception e)
            {
                objConn.Close();

                throw (e);

            }



        }
        public static DataRow DoTransWithDatarowRead(string sQuery, SqlParameter[] sqlPara)
        {
            //	string sDbConn=ConfigurationSettings.AppSettings["SQLConnString1"];

            try
            {
                return DoTransWithDatarowRead(_DbConnString, sQuery, sqlPara);

            }
            catch (System.Exception e)
            {

                throw (e);

            }


        }
         */
        #endregion
        #region GetDataTable
        public static DataTable GetDataTable(string sQuery)
        {


            return GetDataTable(sQuery, _DbConnString);
        }
        public static DataTable GetDataTable(string sQuery, string sDbConn)
        {


            return GetDataTableMain(sQuery, null, sDbConn);

        }
        public static DataTable GetDataTable(string sQuery, Hashtable hashParms)
        {

            return GetDataTable(sQuery, hashParms, _DbConnString);
        }
        public static DataTable GetDataTable(string sQuery, SqlParameter[] sqlParms)
        {
            return GetDataTable(sQuery, sqlParms, _DbConnString);
        }
        public static DataTable GetDataTable(string sQuery, Hashtable hashParms, string sDbConn)
        {
            return GetDataTable(sQuery, ConvertHashTabletoSqlParameterArray(hashParms), sDbConn);
        }

        public static DataTable GetDataTable(string sQuery, SqlParameter[] sqlParms, string sDbConn)
        {
            return GetDataTableMain(sQuery, sqlParms, sDbConn);
        }

        private static DataTable GetDataTableMain(string sQuery, SqlParameter[] sqlParms, string sDbConn)
        {

            DataTable dt = new DataTable();
            using (SqlConnection objConn = new SqlConnection(sDbConn ?? _DbConnString))
            {

                try
                {
                    SqlCommand objCmd = new SqlCommand("", objConn);
                    PrepareCommand(objCmd, sQuery, sqlParms);
                    SqlDataAdapter da = new SqlDataAdapter(objCmd);
                    da.Fill(dt);

                }
                catch (System.Exception e)
                {

                    SetErrorTrace("GetDataSetwithPara	" + sQuery + "	" + e.Message);
                    //	SetErrorTrace(sQuery,e.Message);
                    if (!NoThrowError)
                    throw (e);
                }
                objConn.Close();
                return dt;
            }


        }
        #endregion

        #region Execute
        //new added:
        public static object ExecuteWithReturnValue(string sDbConn, string sQuery, Hashtable htParas)
        {
            if (sDbConn == null)
                sDbConn = _DbConnString;
            using (SqlConnection objConn = new SqlConnection(sDbConn ?? _DbConnString))
            {
                try
                {
                    objConn.Open();
                    SqlCommand objCmd = new SqlCommand("", objConn);
                    SqlParameter[] sqlParas = ConvertHashTabletoSqlParameterArray(htParas);
                    foreach (SqlParameter paraTmp in sqlParas)
                        paraTmp.Direction = ParameterDirection.InputOutput;
                    PrepareCommand(objCmd, sQuery, sqlParas);
                    objCmd.ExecuteNonQuery();
                    htParas.Clear();
                    SqlParameterCollection paraCollection = objCmd.Parameters;
                    foreach (SqlParameter sqlPara in paraCollection)
                        htParas.Add(sqlPara.ParameterName, sqlPara.Value);
                    return htParas;
                }
                catch (System.Exception e)
                {
                    SetErrorTrace("ExcecuteWithPara	" + sQuery + "	" + e.Message);
                    if (!NoThrowError)
                    throw (e);
                }
            }
            return null;
        }

        public static int Execute(string sQuery)
        {
            //	string sDbConn=ConfigurationSettings.AppSettings["SQLConnString1"];

            return Execute(sQuery, _DbConnString);
        }
        public static int Execute(string sQuery, string sDbConn)
        {
            //	string sDbConn=ConfigurationSettings.AppSettings["SQLConnString1"];
            SqlParameter[] sqlParms = null;
            return Execute(sDbConn, sQuery, sqlParms);

        }
        public static int Execute(string sQuery, Hashtable hashParms)
        {

            return  Execute(_DbConnString, sQuery, hashParms);
        }
       
        public static int Execute(string sDbConn, string sQuery, Hashtable hashParms)
        {
            return Execute(sDbConn, sQuery, ConvertHashTabletoSqlParameterArray(hashParms));
        }

        public static int Execute(string sDbConn, string sQuery, SqlParameter[] sqlParms)
        {
            return ExecuteMain(sDbConn, sQuery, sqlParms);
        }

        public static int ExecuteNonQuery(string sqlstr)
        {
            return DoTrans(sqlstr);
        }
        public static int ExecuteNonQuery(string sqlstr, Hashtable ht, string connstr=null)
        {
            return DoTrans(connstr??_DbConnString, sqlstr, ht);
        }
        private static int ExecuteMain(string sDbConn, string sQuery, SqlParameter[] sqlParms)
        {
            using (SqlConnection objConn = new SqlConnection(sDbConn ?? _DbConnString))
            {
                try
                {
                    objConn.Open();
                    using (SqlCommand objCmd = new SqlCommand("", objConn))
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
        #endregion

        #region GetObject/GetInt

        public static object GetObject(string sQuery, Hashtable hashParms, string sDbConn)
        {
            return GetObject(sQuery, ConvertHashTabletoSqlParameterArray(hashParms), sDbConn);
        }

        public static object GetObject(string sQuery, SqlParameter[] sqlParms, string sDbConn)
        {
            return GetObjectMain(sQuery, sqlParms, sDbConn);
        }
        public static object GetObject(string sQuery, string sDbConn)
        {
            SqlParameter[] sParas = null;
            return GetObject(sQuery, sParas, sDbConn);

        }
        public static object GetObject(string sQuery)
        {
            return GetObject(sQuery, _DbConnString);

        }
        public static object GetObject(string sQuery, Hashtable hashParms)
        {
            return GetObject(sQuery, ConvertHashTabletoSqlParameterArray(hashParms), _DbConnString);
        }
        public static object GetObject(string sQuery, SqlParameter[] sqlParms)
        {
            return GetObject(sQuery, sqlParms, _DbConnString);

        }

        private static object GetObjectMain(string sQuery, SqlParameter[] sqlParms, string sDbConn)
        {
            object obj = null;
            using (SqlConnection objConn = new SqlConnection(sDbConn ?? _DbConnString))
            {
                try
                {
                    objConn.Open();
                    SqlCommand objCmd = new SqlCommand("", objConn);
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

        public static int GetInt(string sQuery, Hashtable hashParms, string sDbConn)
        {
            return GetInt(sQuery, ConvertHashTabletoSqlParameterArray(hashParms), sDbConn);
        }

        public static int GetInt(string sQuery, SqlParameter[] sqlParms, string sDbConn)
        {
            return GetIntMain(sQuery, sqlParms, sDbConn);
        }
        public static int GetInt(string sQuery, Hashtable hasParms)
        {
            return GetInt(sQuery, ConvertHashTabletoSqlParameterArray(hasParms), _DbConnString);

        }
        public static int GetInt(string sQuery, SqlParameter[] sqlParms)
        {
            return GetInt(sQuery, sqlParms, _DbConnString);

        }
        public static int GetInt(string sQuery, string sDbConn)
        {
            SqlParameter[] sParas = null;
            return GetInt(sQuery, sParas, sDbConn);

        }
        public static int GetInt(string sQuery)
        {
            return GetInt(sQuery, _DbConnString);

        }

        private static int GetIntMain(string sQuery, SqlParameter[] sqlParms, string sDbConn)
        {
 
            object ob=GetObject(sQuery, sqlParms, sDbConn);
            if (ob == null||ob is DBNull) return 0;
            return Convert.ToInt32(ob);
        }
        #endregion

        #region Log
        private static object olockfile=new  object();
        public static void SetErrorTrace(string sMessage)
        {
            try
            {
                string sDateTag = System.DateTime.Now.ToString("yyyyMMdd");

                string sPath = System.Configuration.ConfigurationManager.AppSettings["LogPath"];
                if (String.IsNullOrWhiteSpace(sPath)) sPath = AppDomain.CurrentDomain.BaseDirectory;

                sPath = Path.Combine(sPath.Replace("//", "/"), "wronglog/" + sDateTag);
                if (!Directory.Exists(sPath))Directory.CreateDirectory(sPath);
              
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
            catch(Exception exp)
            {
				throw new Exception("写sql日志:"+exp.ToString());
            }

        }

        #endregion


        #region PrepareCommand
        private static SqlParameter[] ConvertHashTabletoSqlParameterArray(Hashtable hashTable)
        {
            if (hashTable == null)
                return null;
            int count = hashTable.Count;
            SqlParameter[] sqlParams = new SqlParameter[count];
            int i = 0;
            foreach (DictionaryEntry hashItem in hashTable)
            {
                sqlParams[i] = new SqlParameter("@"+hashItem.Key.ToString().TrimStart('@'), hashItem.Value);
                i++;
            }
            return sqlParams;

        }
        private static void PrepareCommand(SqlCommand objCmd, string sQuery, SqlParameter[] sqlParms)
        {
            PrepareCommand(objCmd, sQuery, sqlParms, CommandType.Text);

        }
        private static void PrepareCommand(SqlCommand objCmd, string sQuery, SqlParameter[] sqlParms, CommandType cmdType)
        {
            objCmd.Parameters.Clear();
            objCmd.CommandText = sQuery;
            objCmd.CommandType = cmdType;//cmdType;
            objCmd.CommandTimeout = CommandTimeout;


            if (sqlParms != null)
            {
                foreach (SqlParameter sqlPara in sqlParms)
                {
                    objCmd.Parameters.Add(sqlPara);
                }
            }

        }
        #endregion
    };

}