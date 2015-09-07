using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Tuna.iCoder.Base.Core
{
    public interface IBizAction
    {
        bool SIMInsert(ModelBase model, params string[] fields);
        bool SIMDelete(string[] keyNames);
        void SIMUpdate(ModelBase model, string[] fields, params string[] keyNames);
        T SIMSearch<T>(T model, string[] keyNames) where T : ModelBase;
    }
}
