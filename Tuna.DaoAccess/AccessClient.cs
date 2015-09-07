using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuna.DaoAccess
{
    /// <summary>
    /// 静态连接效果
    /// </summary>
    public static class AccessClient
    {
        /// <summary>
        /// 数据启动信息
        /// </summary>
        public static bool Runing = false;

        ///// <summary>
        ///// Fill data with nolock. Allow data is dirty read.
        ///// </summary>
        ///// <param name="isDirtyRead"></param>
        ///// <param name="sql"></param>
        //public static string PatchWithNolock(bool isDirtyRead, string sql)
        //{
        //    //Only to operate search data.
        //    if (string.IsNullOrEmpty(sql)
        //        //Insert update delete can not be used dirty read.
        //        || (sql.ToUpper().IndexOf("DELETE ") >= 0 || sql.ToUpper().IndexOf("UPDATE ") >= 0 || sql.ToUpper().IndexOf("INSERT ") >= 0)
        //        || (sql.ToUpper().IndexOf("SP_") == 0 || sql.ToUpper().IndexOf("FN_") == 0)
        //        || isDirtyRead == false
        //        || sql.ToUpper().IndexOf("DBO.") >= 0
        //        ) return sql;

        //    //处理SQL特殊情况
        //    sql = sql.Replace("\r\n", " \r\n").Trim();
        //reReplace:
        //    if (sql.IndexOf("  ") > 0)
        //    {
        //        sql = sql.Replace("FROM\r\n", "FROM ");
        //        sql = sql.Replace("FROM \r\n", "FROM ");
        //        sql = sql.Replace("  ", " ");
        //        goto reReplace;
        //    }

        //    if (DaoTransactionScope.DaoTransactionScopeList.Count == 0)
        //    {
        //        int iStart = 0;
        //    PatchNolock:
        //        //Find char 'from' state, so I can find the table name
        //        int iCount = sql.ToUpper().IndexOf("FROM ", iStart);
        //        int iCount1 = sql.ToUpper().IndexOf(" ", iCount + 7);
        //        if (iCount1 < 0) return sql + " WITH(NOLOCK) ";
        //        int iCount2 = sql.ToUpper().IndexOf(" ", iCount1 + 2);
        //        string tableName = sql.Substring(iCount + 5, iCount1 - iCount - 5);
        //        if (tableName.IndexOf("(") >= 0 || tableName.IndexOf(",") >= 0)
        //        {
        //            iStart = iCount2 + 2;
        //            goto PatchNolock;
        //        }
        //        //取得别名，进行识别别名
        //        string aliasName = sql.Substring(iCount1 + 1, iCount2 - iCount1 - 1).Replace("\r\n", "").Trim();//getAlias(tableName)
        //        if (iCount > 0)
        //        {
        //            int state = 7;
        //            if (("WHERE|INNER|ORDER|OUTER|UNION|LEFT|RIGHT|GROUP|PIVOT|UNPIVOT|APPLY|)|AS".IndexOf(aliasName.ToUpper()) < 0 && aliasName.Length > 1)
        //                || aliasName == getAlias(tableName))
        //            {
        //                state = 6 + tableName.Length + aliasName.Length;
        //            }
        //            int iState1 = sql.ToUpper().IndexOf(" ", iCount + state);
        //            //int iState2 = sql.ToUpper().IndexOf("\n", iCount + state);
        //            //if (iState1 > iState2 && iState2 > 0)
        //            //    iState1 = iState2;

        //            if (iState1 > 0)
        //            {
        //                //Fill Data By Start State.
        //                sql = sql.Substring(0, iState1) + " WITH(NOLOCK) " + sql.Substring(iState1, sql.Length - iState1);
        //                iStart = iState1 + " WITH(NOLOCK) ".Length + 2;
        //                goto PatchNolock;
        //            }
        //        }
        //    }
        //    return sql;
        //}

        ///// <summary>
        ///// 根据表明取得别名
        ///// </summary>
        ///// <param name="tableName"></param>
        ///// <returns></returns>
        //private static string getAlias(string tableName)
        //{
        //    string sResult = "";
        //    if (tableName.IndexOf("_") > 0)
        //    {
        //        string[] strs = tableName.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
        //        for (int i = 0; i < strs.Length; i++)
        //        {
        //            if (strs[0].Trim().Length > 0)
        //                sResult += strs[i].Trim().Substring(0, 1);
        //        }
        //    }
        //    return sResult.ToUpper();
        //}
    }

    /// <summary>
    /// 指定数据表操作方式
    /// </summary>
    [Serializable, Description("指定数据表操作方式。")]
    public enum DataBaseOperate
    {
        [Description("指定更新数据。")]
        Update = 2,
        [Description("指定插入数据。")]
        Insert = 1,
        [Description("删除对应数据。")]
        Delete = 0
    }

    /// <summary>
    /// 指定分隔符
    /// </summary>
    [Serializable, Description("指定分隔符。")]
    public enum SplitChWords
    {
        [Description("逗号。")]
        Dot = 0,
        [Description("AND符号。")]
        And = 1,
        [Description("OR符号。")]
        Or = 2,
        [Description("无符号。")]
        NULL = 3
    }

    /// <summary>
    /// 指定查询模式
    /// </summary>
    [Serializable, Description("执行查询精确还是模糊。")]
    public enum SeacherMode
    {
        [Description("精确。")]
        Single = 0,
        [Description("模糊。")]
        Flozze = 1,
    }


}
