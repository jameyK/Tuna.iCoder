using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Tuna.DaoAccess;
using Tuna.iCoder.Base.Core;

namespace Tuna.iCoder.Base.Imp
{
    public class BizAction : IBizAction
    {
        private DataAccessClient _client = null;
        public BizAction(string connection)
        {
            this._client = new DataAccessClient(false, connection);
        }
        ~BizAction()
        {
            if (this._client != null)
            {
                this._client.Close();
                this._client = null;
            }
        }

        public bool SIMInsert(ModelBase model, params string[] fields)
        {
            if (_client == null)
            {
                throw new Exception("数据库连接不存在");
            }
            if (_client.State != ConnectionState.Closed)
            {
                throw new Exception("数据库连接已关闭，无法处理数据");
            }

            //构成Insert处理模式
            Type modelType = model.GetType();
            string tableName = modelType.Name;
            string fieldsInsert = "", valuesInsert = "";
            Dictionary<string, FieldInfo> fieldInfos = modelType.GetFields().ToDictionary(c => c.Name, c => c); ;
            List<IDataParameter> parameters = new List<IDataParameter>();
            Dictionary<string, string> filedsDictionary = null;
            if (fields != null) filedsDictionary = fields.ToDictionary(c => c, null); 

            foreach (string s in fieldInfos.Keys)
            {
                if (filedsDictionary != null && filedsDictionary.ContainsKey(s))
                {
                    fieldsInsert += string.Format("{0},", s);
                    valuesInsert += string.Format("@{0},", s);
                    parameters.Add(new SqlParameter(string.Format("@{0}", s), fieldInfos[s].GetValue(model)));
                }
            }
            string sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", tableName,
                fieldsInsert.Substring(0, fieldsInsert.Length - 1), valuesInsert.Substring(0, valuesInsert.Length - 1));
            int count = _client.ExecuteNonQuery(sql, parameters.ToArray());
            return (count == 1);
        }

        public bool SIMDelete(string[] keyNames)
        {
            return false;
        }
        public void SIMUpdate(ModelBase model, string[] fields, params string[] keyNames)
        {
        }
        public T SIMSearch<T>(T model, string[] keyNames) where T : ModelBase
        {
            return default(T);
        }

    }
}
