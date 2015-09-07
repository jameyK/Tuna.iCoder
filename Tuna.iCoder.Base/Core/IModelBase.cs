using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuna.iCoder.Base.Core
{
    public abstract class ModelBase
    {
        public string[] PrimaryKeys { get; set; }
        public string[] Indexs { get; set; }

        public object Clone()
        {
            return Activator.CreateInstance(this.GetType());
        }
        //public T Copy()
        //{
            
        //}
    }
}
