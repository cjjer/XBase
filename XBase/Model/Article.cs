using System;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using System.Data;
using System.Runtime.Serialization;
using XBase.SqlHelper;
using XBase.DAO;
using ProtoBuf;

namespace XBase.Model
{
    [ProtoContract]
    //[Serializable, JsonObject(MemberSerialization.OptOut)]

    public class Article
    {
        public delegate void PushHandler(Article a);
        public event PushHandler Push;

        public void OnPush(Article a)
        {
            if (Push != null)
            {
                Push(a);
            }
        }

       


    }
    public enum DBAction
    {
        插入 = 1, 更新 = 2, 自动 = 3
    }


}
