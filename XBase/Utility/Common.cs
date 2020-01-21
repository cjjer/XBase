using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;

namespace XBase.Utility
{
    public static class Common
    {
        private static Regex tagReg = new Regex(@"[\~!@#\$%\^\*\(\)\[\]\{\}<>\?\\\/\']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static System.Text.RegularExpressions.Regex 和谐名称 = new System.Text.RegularExpressions.Regex(@"[^a-z0-9\u4e00-\u9fa5_\-@#$^&\(\)<>~\|\[\]\.]", System.Text.RegularExpressions.RegexOptions.Compiled);
        public static System.Text.RegularExpressions.Regex 和谐名称2 = new System.Text.RegularExpressions.Regex(@"[^\u4e00-\u9fa5a-zA-Z0-9_\-&#\.\(（\)）<>\\:|\^\$@\!\*\,\/\{\""\}\?、一。~]", System.Text.RegularExpressions.RegexOptions.Compiled);
        public static System.Text.RegularExpressions.Regex SqlSafeQuery = new System.Text.RegularExpressions.Regex(@"[^\u4e00-\u9fa5a-zA-Z0-9_\-\.\:]", System.Text.RegularExpressions.RegexOptions.Compiled);
        public static string GetSafeSqlString(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return SqlSafeQuery.Replace(value, "");
        }
        public static string GetClearTagName(string tagsname)
        {
            if (string.IsNullOrEmpty(tagsname)) return null;
            return 和谐名称2.Replace(tagsname, "").Trim();
        }
        public static bool IsDecimal(this object text)
        {
            return new Regex(@"^[+-]?[0-9]+(\.[0-9]+)?$").IsMatch(text.ToString());
        }
        public static string GetWeixinFace(this string pic)
        {
            return GetWeixinFace(pic,64);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="size">64,96、132</param>
        /// <returns></returns>
        public static string GetWeixinFace(this string pic,int size)
        {
            if (string.IsNullOrWhiteSpace(pic)) return null;
            if (pic.EndsWith("/0"))
            {
                pic = pic.Substring(0, pic.Length - 1) + size;
            }
            return pic;
        }
        public static string GetUserfullUrl(string url)
        {
            if (String.IsNullOrEmpty(url)) return null;
            var posi = url.IndexOf("#");
            if (posi > 0)
            {
                url = url.Substring(0, posi);
            }
            return url;
        }
        public static string GetSafeHtmlString(string str)
        {
            if (string.IsNullOrEmpty(str)) return null;
            str = str.Replace("\n", "<br/>");
            str = str.Replace("\t", "&nbsp;&nbsp;&nbsp;");
            str = str.Replace(" ", "&nbsp;");
            return str;
        }
        public static string GetCnStr(string str, int length)
        {
            if (str == null || str.Length == 0 || length < 0)
            {
                return "";
            }

            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(str);
            int n = 0;  //  表示当前的字节数
            int i = 0;  //  要截取的字节数
            for (; i < bytes.GetLength(0) && n < length; i++)
            {
                //  偶数位置，如0、2、4等，为UCS2编码中两个字节的第一个字节
                if (i % 2 == 0)
                {
                    n++;      //  在UCS2第一个字节时n加1
                }
                else
                {
                    //  当UCS2编码的第二个字节大于0时，该UCS2字符为汉字，一个汉字算两个字节
                    if (bytes[i] > 0)
                    {
                        n++;
                    }
                }
            }
            //  如果i为奇数时，处理成偶数
            if (i % 2 == 1)
            {
                //  该UCS2字符是汉字时，去掉这个截一半的汉字
                if (bytes[i] > 0)
                    i = i - 1;
                //  该UCS2字符是字母或数字，则保留该字符
                else
                    i = i + 1;
            }
            return System.Text.Encoding.Unicode.GetString(bytes, 0, i);
        }
        public static long ConvertToTimestamp(DateTime value)
        {
            return (value.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }

        public static DateTime ToDateTime(long unixTimeStamp, DateTimeKind kind)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, kind);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            //// Unix timestamp is seconds past epoch
            //System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
            //if (unixTimeStamp > 2476789460)
            //{
            //    dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();

            //}
            //else
            //{
            //    dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            //}
            //return dtDateTime;


            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(unixTimeStamp + ((unixTimeStamp > 2476789460) ? "0000" : "0000000"));
            TimeSpan toNow = new TimeSpan(lTime);
            DateTime dtResult = dtStart.Add(toNow);
            return dtResult;

        }
        public static string Money_Show(string str)
        {
            if ((String.IsNullOrWhiteSpace(str) || !str.IsDecimal()))
            {
                return "";
            }
            double dec = double.Parse(str);
            return ((dec == 0.0) ? "" : (Math.Round((double)(dec / 1000000.0), 2) + "M"));
        }
        public static int ExistCount(string Sou, string pa)
        {
            if (String.IsNullOrEmpty(Sou) || String.IsNullOrEmpty(pa)) return 0;
            return Regex.Matches(Sou, pa).Count;
        }
        public static string RemoveAllSignChar(string input)
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)
                {
                    c[i] = (char)32; continue;
                }
            }
            //if (c[i]>65280 && c[i]<65375) c[i]=(char)(c[i]-65248); 
            return new string(c);
        }
        private static Regex 网址校验 = new Regex(@"^(http|https|ftp)\://[a-zA-Z0-9\-\.]+(\.[a-zA-Z]){0,3}(:[a-zA-Z0-9]*)?([0-9]+){0,8}/?([a-zA-Z0-9\-\._\?\,\'/\\\+&%\$#\=~;])*[^\.\,\)\(\s)]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static bool IsValidURL(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return 网址校验.IsMatch(url);
        }
        public static bool IsValidUri(string uriString)
        {
            if ((uriString == null) || (uriString.Length == 0)) return false;
            try
            {
                Uri u = new Uri(uriString);

                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

       
        /// 判断一个DataColumnsCollection是否包含知道数组的列
        /// </summary>
        /// <param name="dcc"></param>
        /// <param name="cols"></param>
        /// <returns></returns>
        public static bool ColumnsContains(DataColumnCollection dcc, params string[] cols)
        {
            if (dcc == null || dcc.Count.Equals(0)) return false;
            foreach (string c in cols)
            {
                if (!dcc.Contains(c)) return false;
            }
            return true;
        }
        private static Regex GUIDR = new Regex(@"^[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
        public static bool IsValidGuid(string args)
        {
            if (String.IsNullOrEmpty(args)) return false;
            return GUIDR.IsMatch(args);
        }
        /// <summary>
        /// 由一个对象获取一个字符串
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string GetString(Object o)
        {
            if (null == o) return String.Empty;
            return o.ToString();
        }
        /// <summary>
        /// 判断一个DataSet/DataTable是否是NULL
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool IsNull(DataTable dt)
        {
            return (dt == null || dt.Rows.Count < 1);
        }
        public static bool IsNull(DataSet dt)
        {
            return (dt == null || dt.Tables.Count < 1);
        }
        /// <summary>
        /// MD5 一个字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Md5(string str)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(str));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();

        }

        /// <summary>
        /// 基于Sha1的自定义加密字符串方法：输入一个字符串，返回一个由40个字符组成的十六进制的哈希散列（字符串）。
        /// </summary>
        /// <param name="str">要加密的字符串</param>
        /// <returns>加密后的十六进制的哈希散列（字符串）</returns>
        public static string Sha1(this string str)
        {
            var buffer = Encoding.UTF8.GetBytes(str);
            var data = SHA1.Create().ComputeHash(buffer);

            var sb = new StringBuilder();
            foreach (var t in data)
            {
                sb.Append(t.ToString("X2"));
            }

            return sb.ToString().ToLower();
        }
        /// <summary>
        /// 半角转全角
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToSBC(string input)
        {
            if (String.IsNullOrEmpty(input)) return null;
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 32)
                {
                    c[i] = (char)12288;
                    continue;
                }
                if (c[i] < 127)
                    c[i] = (char)(c[i] + 65248);
            }
            return new string(c);
        }
        /// <summary>
        /// 全角转半角
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToDBC(string input)
        {
            if (String.IsNullOrEmpty(input)) return null;
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)
                {
                    c[i] = (char)32;
                    continue;
                }
                if (c[i] > 65280 && c[i] < 65375)
                    c[i] = (char)(c[i] - 65248);
            }
            return new string(c).Replace("“", "\"").Replace("”", "\"");
        }
        /// <summary>
        /// 智能移除字符串中以input相隔的字符
        /// </summary>
        /// <param name="Args"></param>
        /// <param name="split"></param>
        /// <returns></returns>
        public static string TrimWords(string Args, char split)
        {
            if (String.IsNullOrEmpty(Args)) return String.Empty;
            string temp = split + Args.Trim(split) + split;
            foreach (string subStr in Args.Trim(split).Split(split)) if (temp.Replace(split + subStr + split, "").IndexOf(subStr) != -1) temp = temp.Replace(split + subStr + split, split.ToString());
            return temp.Trim(split);
        }
        /// <summary>
        /// 返回一个int值的数据
        /// </summary>
        /// <param name="intstr"></param>
        /// <returns></returns>
        public static int CInt(object intstr)
        {
            int id = 0;
            if (intstr == null) return id;
            int.TryParse(string.Format("{0}", intstr), out id);
            return id;
            /*
            if (null == intstr) return 0;

            int id = 0;
            try
            {
                if (intstr is System.Decimal || intstr is string || intstr is int || intstr.GetType().Equals(typeof(object)))
                {
                    int.TryParse(intstr.ToString(), out id);
                }
                else
                {
                    id = (int)intstr;
                }
            }
            catch(Exception exp)
            {
            }
            return id;
             * */
        }
        public static Double CDouble(object intstr)
        {
            double db = 0;
            double.TryParse(intstr.ToString(), out db);
            return db;
        }

        /// <summary>
        /// 不精确的移除全部的HTML标签
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string RemoveHtml(string args)
        {
            if (String.IsNullOrEmpty(args)) return String.Empty;
            var a = 移除标签A.Replace(args, "");
            if (String.IsNullOrEmpty(a)) return String.Empty;
            return 移除标签B.Replace(a, "");
        }
        public static string RemoveHtmlSpace(string args)
        {
            args = RemoveHtml(args);
            if (String.IsNullOrEmpty(args)) return String.Empty;
            return Regex.Replace(args, @"\s+", " ");
        }
        private static Regex 移除标签A = new Regex(@"</[^>]+?>", RegexOptions.Compiled);
        private static Regex 移除标签B = new Regex(@"<[^>]+?>", RegexOptions.Compiled);
        private static Regex 移除链接 = new Regex(@"<a\b[^>]+?>(.*?)</a>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static string RemoveLink(string args)
        {
            if (String.IsNullOrEmpty(args)) return String.Empty;
            return 移除链接.Replace(args, "$1");
        }
        /// <summary>
        /// 彻底删掉超链接
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string RemoveAllLink(string args)
        {
            if (String.IsNullOrEmpty(args)) return String.Empty;
            return 移除链接.Replace(args, "");
        }
        public static string FilterChineseWord(string args)
        {
            if (String.IsNullOrEmpty(args)) return String.Empty;
            //return BadWordRegex.Replace(args, "");
            return args;
        }
        private static Regex BadWordRegex = new Regex(System.Configuration.ConfigurationManager.AppSettings.Get("ChineseFuckName") ?? "fuck", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static bool HasFilterChineseWord(string args)
        {
            if (String.IsNullOrEmpty(args)) return false;
            return BadWordRegex.IsMatch(args);
        }



        static Regex regex_ip = new Regex(@"^\d{1,3}[\.]\d{1,3}[\.]\d{1,3}[\.]\d{1,3}$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
        public static bool IsIPAddress(string str1)
        {
            if (str1 == null || str1 == string.Empty || str1.Length < 7 || str1.Length > 15) return false;
            return regex_ip.IsMatch(str1);
        }
        /// <summary>
        /// 剪字
        /// </summary>
        /// <param name="soustrP"></param>
        /// <param name="lenP"></param>
        /// <param name="AppendP"></param>
        /// <returns></returns>
        public static string CutStr(string soustrP, int lenP, string AppendP)
        {
            if (String.IsNullOrEmpty(soustrP)) return null;
            if (lenP >= soustrP.Length) return soustrP.Trim();
            else
            {
                if (String.IsNullOrEmpty(AppendP)) return soustrP.Substring(0, lenP).Trim();
                else return soustrP.Substring(0, lenP).Trim() + AppendP;
            }
        }
        public static string CutStr(string soustrP, int lenP)
        {
            return CutStr(soustrP, lenP, null);
        }
        /// <summary>
        /// 检测一个字符串能否Parse成日期格式
        /// </summary>
        /// <param name="sid"></param>
        /// <returns></returns>
        public static bool IsValidDateTime(string sid)
        {
            bool isTrue = false;
            try
            {
                DateTime Uptime;
                return DateTime.TryParse(sid, out Uptime);

            }
            catch
            {
                isTrue = false;
            }
            return isTrue;
        }
        /// <summary>
        /// 格式化成一个日期
        /// </summary>
        /// <param name="sid"></param>
        /// <returns></returns>
        public static DateTime CDateTime(object sid)
        {
            DateTime Uptime = DateTime.Now;
            if (sid == null) return Uptime;
            DateTime.TryParse(sid.ToString(), out Uptime);
            return Uptime;
        }
        /// <summary>
        /// 获取一个安全的字符串
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string GetSafeChars(string args)
        {
            if (String.IsNullOrEmpty(args)) return String.Empty;
            return System.Text.RegularExpressions.Regex.Replace(args, @"[^\w\-_\+,]", "").Trim();
        }
        /// <summary>
        /// 给一个网址附加一定的get参数
        /// </summary>
        /// <param name="sPath"></param>
        /// <param name="sQuery"></param>
        /// <param name="sValue"></param>
        public static void AppendQueryString(ref string sPath, string sQuery, string sValue)
        {
            if (!String.IsNullOrEmpty(sPath))
            {
                Regex regex1 = null;
                Match match1 = null;
                sPath = sPath.Trim('&').Trim('?').Trim();
                regex1 = new Regex(@"(&|\?)" + @sQuery + "=([^&]*)", RegexOptions.IgnoreCase);
                match1 = regex1.Match(sPath);
                if (match1.Success)
                {
                    sPath = sPath.Replace(match1.Groups[0].ToString(), match1.Groups[1] + sQuery + "=" + sValue);
                }
                else if (sPath.IndexOf("?") != -1)
                {
                    sPath = sPath.Insert(sPath.IndexOf("?") + 1, sQuery + "=" + sValue + "&");
                }
                else
                {
                    sPath = String.Concat(sPath, "?" + sQuery + "=" + sValue);
                }
            }
        }
        public static bool IsValidSessionID(string sid)
        {
            return !String.IsNullOrEmpty(sid) && Regex.IsMatch(sid, @"^([a-zA-Z0-9]{32})$");
        }
       
        /// <summary>
        /// 获取某个xml文件的节点的值
        /// </summary>
        /// <param name="FullXmlPath"></param>
        /// <param name="XPathString"></param>
        /// <returns></returns>
        public static string SelectSingleNodeValue(string FullXmlPath, string XPathString)
        {
            string returnstr = null;
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(FullXmlPath);
                XmlNode objNode = doc.SelectSingleNode(XPathString);
                if (objNode != null)
                    returnstr = objNode.LastChild.InnerText;
            }
            catch (Exception)
            {
            }
            return returnstr;
        }
        /// <summary>
        /// 模拟PHP等下面的time()
        /// </summary>
        /// <returns></returns>
        public static long Time(DateTime date1)
        {
            DateTime date2 = new DateTime(1970, 1, 1, 0, 0, 0);
            date2 = date2.AddSeconds(8 * 60 * 60);
            return (long)Math.Ceiling(date1.Subtract(date2).TotalMilliseconds);
        }
        private static Regex 替换脏字符 = new Regex(@"[^ a-zA-Z\+&0-9-:#""'\:\%\._\u4e00-\u9fa5,，　]", RegexOptions.Compiled);
        private static Regex 永久URLKEY = new Regex(@"^[a-z0-9_\-]{3,50}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static bool IsValidPermalink(string key)
        {
            return (!string.IsNullOrWhiteSpace(key)) && 永久URLKEY.IsMatch(key);
        }
        public static string PreSearchKey(string k)
        {
            if (String.IsNullOrEmpty(k)) return null;
            k = RemoveHtml(k);
            //k = RemoveAllSignChar(k);
            k = 替换脏字符.Replace(k, "").Trim();
            return k;
        }
        /// <summary>
        /// 获取动态信息提示的关联时间
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string ITime(DateTime date2)
        {
            DateTime date1 = DateTime.Now;
            if (date2 > date1) return date2.ToString("yyyy-MM-dd HH:mm");
            var dayc = (date1.Date - date2.Date).TotalDays;
            var hours = (date1 - date2).TotalHours;
            var mins = (date1 - date2).TotalMinutes;
            if (mins < 2)
            {
                return "刚刚";
            }
            if (mins <60)
            {
                return (int)mins + "分钟前";
            }
            if (hours < 10)
            {
                return (int)hours + "小时前";
            }
            if (dayc == 0)
            {
                return "今天" + date2.ToString("HH:mm");
            }
            if (dayc == 1)
            {
                return "昨天" + date2.ToString("HH:mm");
            }
            if (dayc == 2)
            {
                return "前天" + date2.ToString("HH:mm");
            }
            return date2.ToString("yyyy-MM-dd HH:mm");
        }
        public static string ITime(long dt)
        {
            DateTime date2 = new DateTime(1970, 1, 1, 0, 0, 0);
            date2 = date2.AddSeconds((long)dt + 8 * 60 * 60);
            return ITime(date2);
        }
        public static bool IsEmail(string value)
        {
            if (String.IsNullOrEmpty(value)) return false;
            string emailExpression = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]" +
                                     @"{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+)" +
                                     @")([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
            return System.Text.RegularExpressions.Regex.IsMatch(value, emailExpression, RegexOptions.Compiled);
        }
        static Regex b_allmobile = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        static Regex v_mob = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        public static bool IsMobileAgent(string agent)
        {
            if (string.IsNullOrWhiteSpace(agent) || agent.Length<5) return false;

            if ((b_allmobile.IsMatch(agent) || v_mob.IsMatch(agent.Substring(0, 4))))
            {
                return true;
            }
            return false;
        }
        public static bool IsMobile(string mobile)
        {
            if (String.IsNullOrEmpty(mobile)) return false;
            if (!mobile.StartsWith("1")) return false;
            if (mobile.StartsWith("12")) return false;
            return (System.Text.RegularExpressions.Regex.IsMatch(mobile, @"^1\d{10}$", RegexOptions.Compiled));
        }
        private static Regex R全中文 = new Regex(@"^([\u4e00-\u9fa5]){1,}$", RegexOptions.Compiled);
        private static Regex R正常词性 = new Regex(@"^.*$", RegexOptions.Compiled);
        public static bool 正常词性(string hashkey)
        {
            if (String.IsNullOrEmpty(hashkey)) return false;
            // return (System.Text.RegularExpressions.Regex.IsMatch(hashkey, @"^((\w{1,2})|(\w{2}\+\d{1,8}))$", RegexOptions.Compiled));
            return R正常词性.IsMatch(hashkey);
        }
        public static bool 全是中文(string sid)
        {
            if (String.IsNullOrEmpty(sid)) return false;
            return R全中文.IsMatch(sid);
        }
        public static int GetPageCount(int resultCount, int pagesize)
        {
            if (pagesize < 1 || resultCount < 1) return 1;
            return Math.Max(0, (int)Math.Ceiling(resultCount / (double)pagesize));
        }
        public static int ContainCount(string input, string value)
        {
            if (String.IsNullOrEmpty(input) || String.IsNullOrEmpty(value)) return 0;
            int count = 0;

            for (int i = 0; (i = input.IndexOf(value, i)) >= 0; i++)
            {
                count++;
            }

            return count;
        }
        public static Regex NBSP = new Regex(@"^(&nbsp;)+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string GetMatchString(string text, Regex rx, int point)
        {
            if (rx == null) return null;
            Match match = rx.Match(text);
            string word = "";
            if (match.Success && match.Groups.Count > point) word = match.Groups[point].Value;
            if (!String.IsNullOrEmpty(word))
            {
                word = NBSP.Replace(word, "").Trim();
            }
            return word;
        }
        public static string GetMatchString(string text, string pattern, int point)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(pattern)) return String.Empty;
            return GetMatchString(text, new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase), point);
        }
        public static Regex 过滤词 = null;
        public static bool IsChineseFuckName(string src)
        {
            if (String.IsNullOrEmpty(src)) return false;
            if (过滤词 == null)
            {
                string varsetting = System.Configuration.ConfigurationManager.AppSettings.Get("ChineseFuckName");
                if (string.IsNullOrEmpty(varsetting)) return false;
                过滤词 = new Regex(varsetting, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            }
            return 过滤词.IsMatch(src);
        }
        /// <summary>
        /// Unicode编码转换为汉字
        /// </summary>
        /// <param name="regmatch"></param>
        /// <returns></returns>
        public static string Unicode2GB(string src)
        {
            string dst = "";
            string str = "";
            str = src.Substring(2);
            byte[] bytes = new byte[2];
            bytes[1] = byte.Parse(int.Parse(str.Substring(0, 2), System.Globalization.NumberStyles.HexNumber).ToString());
            bytes[0] = byte.Parse(int.Parse(str.Substring(2, 2), System.Globalization.NumberStyles.HexNumber).ToString());
            dst = Encoding.Unicode.GetString(bytes);
            return dst;
        }

        /// <summary>
        /// 返回包含 min 与 max 在内的随机数
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int GetRandomNumber(int min, int max)
        {
            int rtn = 0;
            Random r = new Random();
            byte[] buffer = Guid.NewGuid().ToByteArray();
            int iSeed = BitConverter.ToInt32(buffer, 0);
            r = new Random(iSeed);
            rtn = r.Next(min, max + 1);
            return rtn;
        }
       
        static System.Text.RegularExpressions.Regex _safe = new System.Text.RegularExpressions.Regex(@"[^a-zA-Z0-9_\-]", System.Text.RegularExpressions.RegexOptions.Compiled);
        public static string GetSafeCharString(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return _safe.Replace(value, "");
        }

        public static string GetSafeSqlPart(this string value,bool usestrice=true)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            value = value.Replace("\\","");
            if (usestrice)
            {
                var tmpval=ToDBC(value).ToLower();
                if (tmpval.IndexOf("delete ") != -1) return null;
                if (tmpval.IndexOf("alter ") != -1) return null;
                if (tmpval.IndexOf("select ") != -1) return null;
                if (tmpval.IndexOf("drop ") != -1) return null;
                if (tmpval.IndexOf("select ") != -1) return null;
                if (tmpval.IndexOf("insert ") != -1) return null;
            }
            return value;
        }

        public static int[] GetUsedIntFromEnum(Type es, int input)
        {
            List<int> ids = new List<int>();
            foreach (Enum item in Enum.GetValues(es))
            {
                var cid =int.Parse (Convert.ChangeType(item, item.GetTypeCode()).ToString()); ;
                if ((cid & input) > 0)
                {
                    ids.Add(cid);
                    Console.WriteLine(item.ToString());
                }
            }
            return ids.ToArray();
        }

        public static string GetExcelColId(int index)
        {
            var j = index;
            string pre = "";
            var startN = j / 27;
            if (startN > 0) pre = string.Format("{0}", (char)(startN + 64));
            var endN = string.Format("{0}", (char)(j % 27 + 64 + startN));
            var LOOKC = pre + endN;
            return LOOKC;
        }
        static Regex r字母数字 = new Regex(@"[^a-z0-9A-Z]",RegexOptions.Compiled);
        public static string GetValidId(string args)
        {
            if (String.IsNullOrEmpty(args)) return String.Empty;
            return r字母数字.Replace(args, "").Trim();
        }
        //public static string FormatMoney(double d)
        //{

        //    return String.Format(System.Globalization.CultureInfo.CreateSpecificCulture("en-us"), "{0:C}", d);
        //}
    };
}
