using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Xml.Serialization;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace XBase.Utility
{
    public static class IOHelper
    {
        /// <summary>
        /// 得到一个分页后的SQL语句
        /// </summary>
        /// <param name="表名"></param>
        /// <param name="需要返回的列"></param>
        /// <param name="排序的字段名"></param>
        /// <param name="总条数">请传进来总条数哦</param>
        /// <param name="每页条数"></param>
        /// <param name="当前页数">请保证大于总页数</param>
        /// <param name="排序条件">请不要加Order by语句</param>
        /// <param name="Where条件">请不要再以Where开头</param>
        /// <returns></returns>
        public static string GetPagerSql(string 表名, string 需要返回的列, string 主键名, int 总条数, int 每页条数, int 当前页数, string 排序条件, string Where条件)
        {
            if ((string.IsNullOrEmpty(主键名) || string.IsNullOrEmpty(表名)) || ((总条数 < 1) || (每页条数 < 1)))
            {
                return null;
            }
            当前页数 = Math.Max(1, 当前页数);
            需要返回的列 = 需要返回的列 ?? "*";
            排序条件 = 排序条件 ?? 主键名;
            if (!Regex.IsMatch(排序条件, @"\s(asc|desc)$", RegexOptions.Compiled | RegexOptions.IgnoreCase))
            {
                排序条件 = 排序条件 + " desc";
            }
            if ((当前页数 * 每页条数) > 总条数)
            {
                当前页数 = (int)Math.Ceiling((double)(((double)总条数) / ((double)每页条数)));
            }
            if (string.IsNullOrEmpty(Where条件))
            {
                Where条件 = "1=1";
            }
            if (当前页数 == 1)
            {
                return string.Format("select top {2} {0} from {1}  {4}  order by {3} ", new object[] { 需要返回的列, 表名, 每页条数, 排序条件, string.IsNullOrEmpty(Where条件) ? "" : (" where " + Where条件) });
            }
            Where条件 = Where条件 ?? "1=1";
            return string.Format("select top {0} {1} from {2}  where {3} not in(select top " + (每页条数 * (当前页数 - 1)) + "  {3} from {2} where {4} order by {5} ) and  {4}  order by {5}", new object[] { 每页条数, 需要返回的列, 表名, 主键名, Where条件, 排序条件 });
        }

        private static Regex sqlr = new Regex(@"^[\w\.]+(?<sql>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>
        /// ht的key 的第二部分 为 操作数，例如ht["Name%like%"],ht["Name%like"],ht["Name in(2,3)"],ht["Name&3>0"]
        /// </summary>
        /// <param name="ht"></param>
        /// <param name="htWhere"></param>
        /// <returns></returns>
        public static string GetSqlWhere(Hashtable ht, Hashtable htWhere, bool use_mssql = true)
        {
            string str = string.Empty;
            if ((ht != null) && (htWhere != null))
            {

                int j = 0;
                foreach (DictionaryEntry entry in ht)
                {
                    j++;
                    string input = entry.Key.ToString();
                    Match match = sqlr.Match(input);
                    string keyright2 = "=";
                    if (match.Success)
                    {
                        keyright2 = match.Groups["sql"].Value;
                    }
                    if (string.IsNullOrEmpty(keyright2)) keyright2 = "=";
                    string keyright = keyright2.Trim().ToLower();
                    string keycol = Regex.Replace(input, @"[^\w\.].*$", "");
                    if (keyright.Equals("!="))
                    {
                        string sqlPar = String.Format("{0}{1}", keycol.Replace(".", ""), j);
                        str = str + string.Format("{0} {1} @{2}  and ", keycol, keyright, sqlPar);
                        htWhere[sqlPar] = entry.Value;

                    }else if (keyright.StartsWith("=") || keyright.StartsWith(">") || keyright.StartsWith("<") || keyright.StartsWith("!"))
                    {
                        string sqlPar = String.Format("{0}{1}", keycol.Replace(".", ""), j);
                        str = str + string.Format("{0} {1} @{2}  and ", keycol, keyright, sqlPar);
                        htWhere[sqlPar] = entry.Value;
                    }
                    else if (keyright.Equals("%"))
                    {
                        if (use_mssql)
                        {
                            str = str + string.Format("{0} like @{0}{1}+'%'  and ", keycol, j);
                            htWhere[keycol + j.ToString()] = entry.Value;
                        }
                        else
                        {
                            str = str + string.Format("{0} like @{0}{1}  and ", keycol, j);
                            htWhere[keycol + j.ToString()] = (entry.Value.ToString().Replace("%", "").Replace("'", "")) + "%";
                        }

                    }
                    else if (keyright.Equals("%like%"))
                    {
                        if (use_mssql)
                        {
                            str = str + string.Format("{0} like '%'+@{0}{1}+'%'  and ", keycol, j);
                            htWhere[keycol + j.ToString()] = entry.Value;
                        }
                        else
                        {
                            str = str + string.Format("{0} like @{0}{1}  and ", keycol, j);
                            htWhere[keycol + j.ToString()] = "%" + (entry.Value.ToString().Replace("%", "").Replace("'", "")) + "%";
                        }
                    }
                    else if (keyright.Equals("%nnull"))
                    {
                        str = str + string.Format("{0} is not NULL and ", keycol);
                    }
                    else if (keyright.Equals("%null"))
                    {
                        str = str + string.Format("{0} is  NULL and ", keycol);
                    }
                    else if (keyright.Equals("%like"))
                    {
                        if (use_mssql)
                        {
                            str = str + string.Format("{0} like '%'+@{0}{1}  and ", keycol, j);
                            htWhere[keycol + j.ToString()] = entry.Value;
                        }
                        else
                        {
                            str = str + string.Format("{0} like @{0}{1}  and ", keycol, j);
                            htWhere[keycol + j.ToString()] = "%" + (entry.Value.ToString().Replace("%", "").Replace("'", ""));
                        }

                    }
                    else if (keyright.Equals("%in"))
                    {


                        string v_i = "";
                        if (entry.Value != null)
                        {
                            Type valueType = entry.Value.GetType();
                            if (valueType.IsArray)//&& typeof(object).IsAssignableFrom(valueType.GetElementType())
                            {
                                var p_split = string.Join(",", (object[])entry.Value);
                                p_split = p_split.Replace("'", "");
                                p_split = "'" + p_split.Replace(",", "','") + "'";
                                v_i = p_split;
                            }
                            else
                            {
                                var c_2 = string.Format("{0}", entry.Value).Replace("'", "");
                                c_2 = c_2.Replace(",", "','");
                                v_i = "'" + (c_2) + "'";
                            }
                        }

                        str = str + string.Format("{0} in ({1}) and ", keycol, v_i);

                    }
                    else//其他的情况就直接输入key
                    {
                        str = str + " " + input + " and ";
                    }
                }
            }
            str = str.Trim();
            if (str.EndsWith(" and"))
            {
                str = str.Substring(0, str.Length - 4);
            }
            return str;
        }
        public static string GetSqlUpdate(string tableName, Hashtable ht, Hashtable htWhere)
        {
            string str = string.Empty;
            string setq = String.Empty;
            if (ht != null)
            {
                var kls = new List<string>();
                foreach (var ki in ht.Keys)
                {
                    if (ki.ToString().EndsWith("+"))
                    {
                        var ki_real = ki.ToString().Trim('+');
                        setq += String.Format("{0}={0}+{1},", ki_real, ht[ki]);
                    }
                    else if (ki.ToString().EndsWith("-"))
                    {
                        var ki_real = ki.ToString().Trim('-');
                        setq += String.Format("{0}={0}-{1},", ki_real, ht[ki]);
                    }
                    else if (ki.ToString().EndsWith("*"))
                    {
                        var ki_real = ki.ToString().Trim('*');
                        setq += String.Format("{0}={0}*{1},", ki_real, ht[ki]);
                    }
                    else if (ki.ToString().EndsWith("/"))
                    {
                        var ki_real = ki.ToString().Trim('/');
                        setq += String.Format("{0}={0}/{1},", ki_real, ht[ki]);
                    }
                    else
                    {
                        setq += String.Format("{0}=@N__{0},", ki);
                        kls.Add(ki.ToString());
                    }
                }
                foreach (var ki in kls)
                {
                    ht["N__" + ki] = ht[ki];
                }
            }
            setq = setq.Trim(',');
            if ((ht != null) && (htWhere != null))
            {

                int j = 0;

                foreach (DictionaryEntry entry in htWhere)
                {
                    j++;
                    string input = entry.Key.ToString();
                    Match match = sqlr.Match(input);
                    string keyright2 = "=";
                    if (match.Success)
                    {
                        keyright2 = match.Groups["sql"].Value;
                    }
                    if (string.IsNullOrEmpty(keyright2)) keyright2 = "=";
                    string keyright = keyright2.Trim().ToLower();
                    string keycol = Regex.Replace(input, @"[^\w].*$", "");
                    if (keyright.StartsWith("=") || keyright.StartsWith(">") || keyright.StartsWith("<") || keyright.StartsWith("!"))
                    {
                        str = str + string.Format("{0} {1} @{0}{2}  and ", keycol, keyright, j);
                        ht[keycol + j.ToString()] = entry.Value;
                    }
                    else if (keyright.Equals("like%"))
                    {
                        str = str + string.Format("{0} like @{0}{1}+'%'  and ", keycol, j);
                        ht[keycol + j.ToString()] = entry.Value;
                    }
                    else if (keyright.Equals("%like%"))
                    {
                        str = str + string.Format("{0} like '%'+@{0}{1}+'%'  and ", keycol, j);
                        ht[keycol + j.ToString()] = entry.Value;
                    }
                    else if (keyright.Equals("%like"))
                    {
                        str = str + string.Format("{0} like '%'+@{0}{1}  and ", keycol, j);
                        ht[keycol + j.ToString()] = entry.Value;
                    }
                    else if (keyright.Equals("%nnull"))
                    {
                        str = str + string.Format("{0} is not NULL and ", keycol);
                    }
                    else if (keyright.Equals("%null"))
                    {
                        str = str + string.Format("{0} is  NULL and ", keycol);
                    }
                    else if (keyright.Equals("%in"))
                    {
                        string v_i = "";
                        if (entry.Value != null)
                        {

                            Type valueType = entry.Value.GetType();
                            if (valueType.IsArray)//&& typeof(object).IsAssignableFrom(valueType.GetElementType())
                            {
                                var p_split = string.Join(",", (object[])entry.Value);
                                p_split = p_split.Replace("'", "");
                                p_split = "'" + p_split.Replace(",", "','") + "'";
                                v_i = p_split;
                            }
                            else
                            {
                                var c_2 = string.Format("{0}", entry.Value).Replace("'", "");
                                c_2 = c_2.Replace(",", "','");
                                v_i = "'" + (c_2) + "'";
                            }
                        }

                        str = str + string.Format("{0} in ({1}) and ", keycol, v_i);
                        //ht[keycol + j.ToString()] = entry.Value;
                    }
                    else//其他的情况就直接输入key
                    {
                        str = str + " " + input + " and ";
                    }
                }
            }
            str = str.Trim();
            if (str.EndsWith(" and"))
            {
                str = str.Substring(0, str.Length - 4);
            }
            return String.Format("Update {0} set {1} where {2}", tableName, setq, str);
        }


        /// <summary>
        /// 得到一个合理的文件名称
        /// </summary>
        /// <param name="sFileName"></param>
        /// <returns></returns>
        public static string GetValidFileName(string sFileName)
        {
            if (String.IsNullOrEmpty(sFileName)) return null;
            foreach (char lDisallowed in Path.GetInvalidFileNameChars())
            {
                sFileName = sFileName.Replace(lDisallowed.ToString(), "");
            }
            foreach (char lDisallowed in Path.GetInvalidPathChars())
            {
                sFileName = sFileName.Replace(lDisallowed.ToString(), "");
            }
            return sFileName;
        }

        /* - - - - - - - - - - - - - - - - - - - - - - - - 
         * Stream 和 byte[] 之间的转换
         * - - - - - - - - - - - - - - - - - - - - - - - */
        /// <summary>
        /// 将 Stream 转成 byte[]
        /// </summary>
        public static byte[] StreamToBytes(Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);

            // 设置当前流的位置为流的开始
            stream.Seek(0, SeekOrigin.Begin);
            return bytes;
        }

        public static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = stream.Position;

            byte[] readBuffer = new byte[4096];

            int totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead == readBuffer.Length)
                {
                    int nextByte = stream.ReadByte();
                    if (nextByte != -1)
                    {
                        byte[] temp = new byte[readBuffer.Length * 2];
                        Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                        Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                        readBuffer = temp;
                        totalBytesRead++;
                    }
                }
            }

            byte[] buffer = readBuffer;
            if (readBuffer.Length != totalBytesRead)
            {
                buffer = new byte[totalBytesRead];
                Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
            }
            return buffer;
        }


        /// <summary>
        /// 将 byte[] 转成 Stream
        /// </summary>
        public static Stream BytesToStream(byte[] bytes)
        {
            Stream stream = new MemoryStream(bytes);
            return stream;
        }


        /* - - - - - - - - - - - - - - - - - - - - - - - - 
         * Stream 和 文件之间的转换
         * - - - - - - - - - - - - - - - - - - - - - - - */
        /// <summary>
        /// 将 Stream 写入文件
        /// </summary>
        public static void StreamToFile(Stream stream, string fileName)
        {
            // 把 Stream 转换成 byte[]
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            // 设置当前流的位置为流的开始
            stream.Seek(0, SeekOrigin.Begin);

            var dname = Path.GetDirectoryName(fileName);
            if (!System.IO.Directory.Exists(dname))
            {
                System.IO.Directory.CreateDirectory(dname);
            }

            // 把 byte[] 写入文件
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(bytes);
                bw.Close();
                fs.Close();
            }
        }

        /// <summary>
        /// 从文件读取 Stream
        /// </summary>
        public static Stream FileToStream(string fileName)
        {
            // 打开文件
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            // 读取文件的 byte[]
            byte[] bytes = new byte[fileStream.Length];
            fileStream.Read(bytes, 0, bytes.Length);
            fileStream.Close();
            // 把 byte[] 转换成 Stream
            Stream stream = new MemoryStream(bytes);
            return stream;
        }
        public static string XmlSerializerObject(object o)
        {
            if (o == null) return null;
            XmlSerializer xs = new XmlSerializer(o.GetType());
            MemoryStream stream = new MemoryStream();

            xs.Serialize(stream, o);
            stream.Close();
            return System.Text.Encoding.UTF8.GetString(stream.GetBuffer());
        }
        /// <summary> 
        /// 拷贝目录里的文件 
        /// </summary> 
        /// <param name=\"sourceFilePath\">源文件目录</param> 
        /// <param name=\"destFilePath\">目地文件目录</param> 
        public static void CopyDirs(string sourceFilePath, string destFilePath)
        {
            if (Directory.Exists(sourceFilePath))
            {
                // 检查目标目录是否以目录分割字符结束如果不是则添加 
                if (destFilePath[destFilePath.Length - 1] != Path.DirectorySeparatorChar)
                    destFilePath += Path.DirectorySeparatorChar;

                // 判断目标目录是否存在如果不存在则新建 
                // 得到源目录的文件列表,该里面是包含文件以及目录路径的一个数组 
                // 如果你指向copy目标文件下面的文件而不包含目录请使用下面的方法 
                // string[] fileList = Directory.GetFiles(sourceFilePath);    
                if (!Directory.Exists(destFilePath))
                    Directory.CreateDirectory(destFilePath);

                string[] fileList = Directory.GetFileSystemEntries(sourceFilePath);

                // 遍历所有的文件和目录 
                foreach (string file in fileList)
                {
                    // 先当作目录处理如果存在这个目录就递归Copy该目录下面的文件，否则直接Copy文件 
                    if (Directory.Exists(file))
                        CopyDirs(file, destFilePath + Path.GetFileName(file));
                    else
                        System.IO.File.Copy(file, destFilePath + Path.GetFileName(file), true);
                }
            }
        }
        public static object XmlDeserializeObject(Type type, string xml)
        {

            if (string.IsNullOrEmpty(xml)) return null;
            XmlSerializer xs = new XmlSerializer(type);
            StringReader stream = new StringReader(xml);
            return xs.Deserialize(stream);
        }

        public static T XmlDeserialize<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return default(T);
            Type type = typeof(T);
            XmlSerializer xs = new XmlSerializer(type);
            StringReader stream = new StringReader(xml);
            return (T)xs.Deserialize(stream);
        }
        public static Stream ToStream(this string @this)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(@this);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static T ParseXML<T>(this string @this) where T : class
        {
            var reader = XmlReader.Create(@this.Trim().ToStream(), new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment });
            return new XmlSerializer(typeof(T)).Deserialize(reader) as T;
        }
        public static bool SaveStringToFile(string str, string filename)
        {
            return SaveStringToFile(str, filename, false, System.Text.Encoding.UTF8);
        }
        public static bool SaveStringToFile(string str, string filename, bool append)
        {
            return SaveStringToFile(str, filename, false, System.Text.Encoding.UTF8);
        }
        public static bool SaveStringToFile(string str, string filename, bool append, System.Text.Encoding encoding)
        {
            if (String.IsNullOrEmpty(str) || String.IsNullOrEmpty(filename)) return false;
            string dir = Path.GetDirectoryName(filename);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            using (StreamWriter sw = new StreamWriter(filename, append, encoding))
            {
                sw.WriteLine(str);
                sw.Flush();
            }
            return true;
        }

        public static string GetDirNameFromPermalink(string permalink)
        {
            if (String.IsNullOrEmpty(permalink)) return null;
            string pathsplit = Regex.Replace(permalink.ToLower(), "[^0-9a-z]", "");
            if (pathsplit.Length > 6) pathsplit = pathsplit.Substring(0, 6);
            return Regex.Replace(pathsplit, @"(.{1})", "$1/");
        }
       

        public static string ExecuteFile(string files, string args, int nTimeout = 180)
        {
            if (!System.IO.File.Exists(files)) return String.Empty;
            System.Diagnostics.Process myprocess = new System.Diagnostics.Process();
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = files;
                    process.StartInfo.Arguments = args;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();

                    using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                    using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                output.AppendLine(e.Data);
                            }
                        };
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            {
                                error.AppendLine(e.Data);
                            }
                        };

                        process.Start();

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        int timeout = nTimeout * 1000;
                        if (process.WaitForExit(timeout) &&
                            outputWaitHandle.WaitOne(timeout) &&
                            errorWaitHandle.WaitOne(timeout))
                        {
                            return output.ToString();
                            // Process completed. Check process.ExitCode here.
                        }
                        else
                        {
                            Console.WriteLine(error.ToString());
                            // Timed out.
                        }
                    }
                }


                //var startInfo = new System.Diagnostics.ProcessStartInfo(files);
                //startInfo.UseShellExecute = false;
                //if (!String.IsNullOrEmpty(args))
                //{
                //    startInfo.RedirectStandardInput = true;
                //    startInfo.Arguments = args;
                //}
                //startInfo.CreateNoWindow = true;
                //startInfo.RedirectStandardOutput = true;
                //startInfo.RedirectStandardError = true;
                //startInfo.ErrorDialog
                ////myprocess.EnableRaisingEvents = false; 
                ////myprocess.Exited +=new EventHandler(Rsync_Exited);
                //myprocess.StartInfo = startInfo;

                //myprocess.Start();
                ////StreamWriter myStreamWriter = myprocess.StandardInput;
                ////myStreamWriter.WriteLine(args);
                ////myStreamWriter.Write("logout" + System.Environment.NewLine);
                ////myStreamWriter.Close();
                ////myprocess.WaitForExit();
                //return myprocess.StandardOutput.ReadToEnd();

            }
            catch (Exception e0)
            {
                Console.WriteLine("启动应用程序时出错！原因：" + e0.Message);
            }
            return String.Empty;
        }
        static String File2HtmlParseExe = System.Configuration.ConfigurationManager.AppSettings["File2HtmlParseExe"];

        /// <summary>
        /// 自动获取本地一个OFFICE文件的HTML格式文件
        /// </summary>
        /// <param name="localpath"></param>
        /// <returns></returns>
        public static string Auto2Html(string localpath, string remoteDir)
        {
            if (String.IsNullOrWhiteSpace(localpath) || !System.IO.File.Exists(localpath)) return null;
            localpath = System.IO.Path.GetFullPath(localpath);
            if (!localpath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return null;
            if (String.IsNullOrWhiteSpace(File2HtmlParseExe)) return null;
            string resultFile = null;
            string existsName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(localpath), System.IO.Path.GetFileNameWithoutExtension(localpath) + @"\index.html");
            if (System.IO.File.Exists(existsName))
            {
                resultFile = existsName;
            }
            else
            {
                var args = "2html \"" + localpath + "\"";
                string executeString = IOHelper.ExecuteFile(File2HtmlParseExe, args);
                if (String.IsNullOrWhiteSpace(executeString)) return null;
                Console.WriteLine(executeString);
                var matchX = System.Text.RegularExpressions.Regex.Match(executeString, @"2HtmlOK#(.*?)(\n|$)");

                if (!matchX.Success) return null;
                resultFile = matchX.Groups[1].Value.Trim();
            }
            if (String.IsNullOrWhiteSpace(resultFile)) return null;
            if (!System.IO.File.Exists(resultFile)) return null;
            //string resultHtml = System.IO.File.ReadAllText(resultFile, Encoding.Default);
            //resultHtml = (MyUtility.Common.ToDBC(resultHtml));
            ////开始用正则替换相关文本@
            //resultHtml = MyUtility.Common.GetMatchString(resultHtml, @"<BODY.*?>([?:\s\S]+?)</BODY>", 1);
            //resultHtml = Regex.Replace(resultHtml, @"src=""(\w+).png", String.Format(@"src=""{0}$1.png",remoteDir));

            string resultHtml = remoteDir + "index.html";
            return resultHtml;
        }
        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        public static void DeleteDir(string dirname)
        {
            if (!System.IO.Directory.Exists(dirname)) return;
            System.IO.DirectoryInfo di = new DirectoryInfo(dirname);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
            System.IO.Directory.Delete(dirname);
        }
        public static bool ByteArrayToFile(string _FileName, byte[] _ByteArray)
        {
            try
            {
                // Open file for reading
                System.IO.FileStream _FileStream =
                   new System.IO.FileStream(_FileName, System.IO.FileMode.Create,
                                            System.IO.FileAccess.Write);
                // Writes a block of bytes to this stream using data from
                // a byte array.
                _FileStream.Write(_ByteArray, 0, _ByteArray.Length);

                // close file stream
                _FileStream.Close();

                return true;
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}",
                                  _Exception.ToString());
            }

            // error occured, return false
            return false;
        }

    }
}
