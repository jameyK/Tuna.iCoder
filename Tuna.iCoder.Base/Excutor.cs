using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tuna.iCoder.Base.Core;
using Tuna.iCoder.Base.Imp;
using Tuna.iCoder.Base.Unility;

namespace Tuna.iCoder.Base
{
    public static class Excutor
    {
        private static SqlConfigAttach config = null;
        private static string _connStr = null;
        public static void LoadSetting(string configpath, string connectionStr)
        {
            _connStr = connectionStr;
            SqlConfigAttach configTemp = ConfigurationManagers<SqlConfigAttach>.Instance.GetConfiguration(configpath);
            if (configTemp != null)
            {
                if (config == null)
                    config = configTemp;
                else
                {
                    foreach (TableMap t in configTemp.TableMaps)
                        config.TableMaps.Add(t);
                }
            }
        }

        public static void Excute<T>(T model, string actionName, Delegate callback = null) where T : ModelBase
        {
            if (config == null)
            {
                throw new Exception("配置文件SqlConfigAttach加载失败");
            }

            if (model == null)
            {
                throw new Exception("数据信息不能为null");
            }

            if (string.IsNullOrEmpty(actionName))
            {
                throw new Exception("数据操作处理对象名不能为null");
            }

            Type type = typeof (T);
            var tableMap = config.TableMaps.FirstOrDefault(c => type.Name.Equals(c.Name));
            Operation operation = null;
            if (tableMap != null)
            {
                operation = tableMap.Operations.FirstOrDefault(c => actionName.Equals(c.Name));
                OperationType oprType = OperationType.Insert;
                OperationType.TryParse(operation.Type, true, out oprType);
                IBizAction action = new BizAction(_connStr);
                object result = null;
                switch (oprType)
                {
                    case OperationType.Search:
                        result = action.SIMSearch<T>(model,
                            string.IsNullOrEmpty(operation.KeyNames) ? null : operation.KeyNames.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case OperationType.Insert:
                        result = action.SIMInsert(model);
                        break;
                    case OperationType.Update:
                        action.SIMUpdate(model,
                            string.IsNullOrEmpty(operation.Fields) ? null : operation.Fields.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries),
                            string.IsNullOrEmpty(operation.KeyNames) ? null : operation.KeyNames.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case OperationType.Delete:
                        action.SIMDelete(string.IsNullOrEmpty(operation.KeyNames)
                            ? model.PrimaryKeys
                            : operation.KeyNames.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries));
                        break;
                }

                if (callback != null && result != null) 
                    callback.DynamicInvoke(oprType, result);
            }

        }
    }
}
