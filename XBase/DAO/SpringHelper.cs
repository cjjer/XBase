using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XBase.DAO
{
    public static class SpringHelper
    {
        public static string GetForkColumn(this string col)
        {
            return string.Format("|{0}|", col);
        }
    }
}
