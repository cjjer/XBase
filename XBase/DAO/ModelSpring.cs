using System;
using System.Collections.Generic;
using System.Text;

namespace XBase.DAO
{
    //重要，修改必须明白
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class ModelSpring : Attribute
    {
        public Type Name;

        public ModelSpring(Type modelname)
        {
            this.Name = modelname;
        }

        public override string ToString()
        {
            return String.Format("对应的模型名称是", Name.FullName);
        }


    }
}
