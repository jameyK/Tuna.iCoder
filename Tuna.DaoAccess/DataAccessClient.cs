using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Tuna.DaoAccess
{
    /// <summary>
    /// 当前新SQL执行方法
    /// </summary>
    public class DataAccessClient : IDisposable
    {
        #region 常量、变量定义
 
        private string _dBConnectString; //private static string DBConnectString;
        private int _dBCommandTimeOut = 20; //超时时间为10分钟 private static int DBCommandTimeOut = 0;
        private bool _isTransaction = false;
        private SqlTransaction _dbTransaction = null;
        private bool _isCatchLog = true;
        private bool _disposed = false;
        private SqlConnection _mDbConnection = null;
        private string _sqlOperateUser = null;
        private bool _isDirtyRead = false;
        private const int _maxPool = 400;
        private const int _minPool = 15;
        private const int _conn_Timeout = 15;
        private const int _conn_Lifetime = 15;
        private const bool _asyn_Process = true;

        #endregion 常量、变量定义

        #region 构造函数

        /// <summary>
        /// 类构造函数 int commandTimeOut，无加密连接
        /// </summary>
        /// <param name="DBConnectString">数据库连接信息</param>
        public DataAccessClient(bool isDirtyRead, string DBConnectString)
            : this(isDirtyRead, DBConnectString, default(int))
        {
        }
 
        /// <summary>
        /// 类构造函数 int commandTimeOut，无加密连接
        /// </summary>
        /// <param name="DBConnectString">连接字符串，ConnectionDataSource枚举值</param>
        /// <param name="commandTimeOut">超时时间</param>
        public DataAccessClient(bool isDirtyRead, string DBConnectString, int commandTimeOut)
        {
            //清理SQL语句
            this.lastSql = null;
            if (true || AccessClient.Runing)
            {
                this.IsDirtyRead = isDirtyRead;
                this._dBCommandTimeOut = commandTimeOut;

                //兼容Sql和SQLCOMMAND模式
                this.DBConnectString = DBConnectString
                    + "Max Pool Size=" + _maxPool + ";"
                    + "Min Pool Size=" + _minPool + ";"
                    + "Connect Timeout=" + _conn_Timeout + ";"
                    + "Connection Lifetime=" + _conn_Lifetime + ";"
                    + "Asynchronous Processing=" + _asyn_Process + ";";

                //数据库连接字符串常量
                this._mDbConnection = new SqlConnection(this.DBConnectString);
                this._mDbConnection.Open();
            }
        }
        #endregion

        #region 属性

        /// <summary>
        /// 最后执行的SQL
        /// </summary>
        private StringBuilder _lastSql = new StringBuilder();

        /// <summary>
        /// 最后执行SQL语句
        /// </summary>
        public string lastSql
        {
            get
            {
                return _lastSql.ToString();
            }
            set
            {
                try
                {
                    if (_lastSql == null) _lastSql = new StringBuilder();
                    if (value == null)
                    {
                        _lastSql.Remove(0, _lastSql.Length);
                    }
                    else
                    {
                        if (!this._isCatchLog) return;
                        if (value.ToUpper().Contains("INSERT ") || value.ToUpper().Contains("UPDATE ") || value.ToUpper().Contains("DELETE "))
                        {
                            string SqlTmp = value.Replace("\n", " ").Replace("\r", " ").Replace("  ", " ");
                            if (_lastSql.ToString().Contains(SqlTmp) == true)
                                return;

                            _lastSql.AppendLine(SqlTmp);
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        }

        /// <summary>
        /// 是否记录日志数据
        /// </summary>
        [Description("是否记录日志数据"), DefaultValue(true)]
        public bool IsCatchLog { get { return this._isCatchLog; } set { this._isCatchLog = value; } }
 
        /// <summary>
        /// 是否进行数据脏读
        /// </summary>
        public

            bool IsDirtyRead
        {
            get { return this._isDirtyRead; }
            set { this._isDirtyRead = value; }
        }
 
        /// <summary>
        /// 外部设定DB连接字符串
        /// </summary>
        public string DBConnectString
        {
            get
            {
                return _dBConnectString;
            }
            private set
            {
                this._dBConnectString = value;
            }
        }

        /// <summary>
        /// 属性前缀
        /// </summary>
        protected string ParameterPrefixStringInSql
        {
            get
            {
                //目前只支持SQLServer
                //if (this.DataProvider != Inf.DevLib.Data.DataAccess.DataProvider.MicrosoftJetOLEDB)
                //{
                //    if (this.DataProvider == Inf.DevLib.Data.DataAccess.DataProvider.SQLOLEDB)
                //    {
                //        return ":";
                //    }
                //    if (this.DataProvider == Inf.DevLib.Data.DataAccess.DataProvider.MSDAORA)
                //    {
                //        return "@";
                //    }
                //}
                return "@";
            }
        }

        /// <summary>
        /// 返回真值
        /// </summary>
        protected int TrueValue
        {
            get
            {
                //if (this.DataProvider == Inf.DevLib.Data.DataAccess.DataProvider.MicrosoftJetOLEDB)
                //{
                //    return -1;
                //}
                //if ((this.DataProvider != Inf.DevLib.Data.DataAccess.DataProvider.SQLOLEDB) && (this.DataProvider == Inf.DevLib.Data.DataAccess.DataProvider.MSDAORA))
                //{
                //    return 1;
                //}
                return 1;
            }
        }

        /// <summary>
        /// 返回假值
        /// </summary>
        protected int FalseValue
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// 事务对象
        /// </summary>
        public bool IsTransaction
        {
            get
            {
                return this._isTransaction;
            }
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        public ConnectionState State
        {
            get
            {
                return _mDbConnection.State;
            }
        }

        /// <summary>
        /// 事务公开属性
        /// </summary>
        protected IDbTransaction TransactionInstance
        {
            get
            {
                return this._dbTransaction;
            }
        }

        /// <summary>
        /// 资源是否已回收
        /// </summary>
        public bool Disposed
        {
            get
            {
                return this._disposed;
            }
        }
        #endregion

        #region 方法实现

        #region 打开和关闭数据库连接

        /// <summary>
        /// 打开DB连接
        /// </summary>
        /// <remarks>modified by 张京军 2010-12-24</remarks>
        public void OpenConnection()
        {
            SqlConnection DBConnect = null;
            try
            {
                DBConnect = new SqlConnection(DBConnectString);
                DBConnect.Open();

                _mDbConnection = DBConnect;
            }
            catch
            {
                throw;
            }
        }


        ///****************************************************************************************************
        /// 函 数 名：CloseConnection
        /// 输入参数：SqlConnection DBConnect:	数据库连接对象
        /// 返回值  ：无
        /// 功能描述：
        /// <summary>
        /// 关闭DB连接
        /// </summary>
        /// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  
        /// 创 建 人：张京军                          创建日期：2010-11-23
        /// 修 改 人：                              修改日期：
        ///****************************************************************************************************
        public void CloseConnection(SqlConnection DBConnect)
        {
            if (DBConnect != null)
            {
                DBConnect.Close();
                DBConnect.Dispose();
            }
        }

        #endregion 打开和关闭数据库连接

        #region 事务对象调用方法
        ///****************************************************************************************************
        /// 函 数 名：BeginTransaction
        /// 输入参数：无
        /// 功能描述：
        /// <summary>
        /// 创建并事务对象
        /// </summary>
        /// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  
        /// 创 建 人：张京军                        创建日期：2010-11-23
        /// 修 改 人：                              修改日期：
        ///****************************************************************************************************
        public void BeginTransaction()
        {
            BeginTransaction(IsolationLevel.ReadCommitted);
        }

        /// <summary>
        /// 开启事务，指定事务锁定范围
        /// </summary>
        /// <param name="isolationlevel"></param>
        public void BeginTransaction(IsolationLevel isolationlevel)
        {
            if (_mDbConnection.State != ConnectionState.Open)
            {
                OpenConnection();
            }
            //SqlTransaction Transaction = DBConnect.BeginTransaction();
            //return Transaction;
            this._dbTransaction = this._mDbConnection.BeginTransaction(isolationlevel);
            this._isTransaction = true;

            //将实例提交给事务
            bool Flag = true;
            string CurrentFlag = "";
            for (int i = 0; i < DaoTransactionScope.DaoTransactionScopeList.Count; i++)
            {
                if (DaoTransactionScope.DaoTransactionScopeList[i].DataAccessInstance == null)
                {
                    DaoTransactionScope.DaoTransactionScopeList[i].DataAccessInstance = this;
                }
                else if (DaoTransactionScope.DaoTransactionScopeList[i].DataAccessInstance == this)
                { Flag = true; }
                else
                { Flag = false; CurrentFlag = DaoTransactionScope.DaoTransactionScopeList[i].CurrentFlag; }
            }

            //创建新事务信息
            if (!Flag)
            {
                DaoTransactionScope ts = new DaoTransactionScope(true);
                ts.DataAccessInstance = this;
                ts.CurrentFlag = CurrentFlag;
            }
        }

        ///****************************************************************************************************
        /// 函 数 名：CommitTransaction
        /// 输入参数：SqlTransaction Transaction:要提交的事务对象
        /// 返回值  ：无
        /// 功能描述：
        /// <summary>
        /// 提交事务对象
        /// </summary>
        /// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  
        /// 创 建 人：张京军                          创建日期：2010-11-23
        /// 修 改 人：                              修改日期：
        ///****************************************************************************************************
        public void CommitTransaction()
        {
            //if (Transaction != null)
            //{
            //    SqlConnection DBConnect = Transaction.Connection;
            //    Transaction.Commit();
            //    CloseConnection(DBConnect);
            //}
            if (!this.IsTransaction)
            {
                throw new InvalidOperationException("事务未能开启");
            }
            this._dbTransaction.Commit();
            this._dbTransaction.Dispose();
            this._dbTransaction = null;
            this._isTransaction = false;
        }

        ///****************************************************************************************************
        /// 函 数 名：RollbackTransaction
        /// 输入参数：SqlTransaction Transaction:要回滚的事务对象
        /// 返回值  ：无
        /// 功能描述：
        /// <summary>
        /// 回滚事务对象
        /// </summary>
        /// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -  
        /// 创 建 人：张京军                          创建日期：2010-11-23
        /// 修 改 人：                              修改日期：
        ///****************************************************************************************************
        public void RollbackTransaction()
        {
            //if ((Transaction != null) && (Transaction.Connection != null))
            //{
            //    SqlConnection DBConnect = Transaction.Connection;
            //    Transaction.Rollback();
            //    CloseConnection(DBConnect);
            //}
            if (!this.IsTransaction)
            {
                throw new InvalidOperationException("事务未能开启");
            }
            this._dbTransaction.Rollback();
            this._dbTransaction.Dispose();
            this._dbTransaction = null;
            this._isTransaction = false;
        }
 
        #endregion

        #region 执行SQL语句
  
        /// <summary>
        /// 执行SQL 返回IDataReader对象
        /// </summary>
        /// <param name="selectSql">查询语句</param>
        /// <param name="commandParameters">参数</param>
        public IDataReader ExecuteDataReader(string selectSql, IDataParameter[] commandParameters)
        {
            return this.ExecuteDataReaderImpl(CommandType.Text, selectSql, commandParameters);
        }

        /// <summary>
        /// 执行SQL 返回IDataReader对象
        /// </summary>
        /// <param name="selectSql">查询语句</param>
        /// <param name="commandParameters">参数</param>
        public IDataReader ExecuteDataReader(string selectSql, params object[] parameterNameAndValues)
        {
            return this.ExecuteDataReader(selectSql, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        /// 执行存储过程 返回IDataReader对象
        /// </summary>
        /// <param name="selectSql">存储过程名称</param>
        /// <param name="commandParameters">参数</param>
        public IDataReader ExecuteDataReaderBySP(string spName, IDataParameter[] commandParameters)
        {
            return this.ExecuteDataReaderImpl(CommandType.StoredProcedure, spName, commandParameters);
        }

        /// <summary>
        /// 执行存储过程 返回IDataReader对象
        /// </summary>
        /// <param name="selectSql">存储过程名称</param>
        /// <param name="commandParameters">参数</param>
        public IDataReader ExecuteDataReaderBySP(string spName, params object[] parameterNameAndValues)
        {
            return this.ExecuteDataReaderBySP(spName, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        /// 执行命令 返回IDataReader对象
        /// </summary>
        /// <param name="commandType">命令类型枚举</param>
        /// <param name="commandText">执行命令</param>
        /// <param name="commandParameters">参数</param>
        protected virtual IDataReader ExecuteDataReaderImpl(CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            IDataReader reader2;
            //Search data with nolock.
            IDbCommand command = this.GetCommand(commandType, commandText, commandParameters);
            IDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                command.Parameters.Clear();
                command.Dispose();
                reader2 = reader;
            }
            catch
            {
                if (reader != null)
                {
                    try
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                    catch { }
                }
                try
                {
                    command.Parameters.Clear();
                    command.Dispose();
                }
                catch { }
                throw;
            }
            return reader2;
        }

        /// <summary>
        /// 返回一个DataSet数据
        /// </summary>
        /// <param name="selectSql">SQL语句</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string selectSql, IDataParameter[] commandParameters)
        {
            DataSet dataSet = new DataSet();
            dataSet.Locale = CultureInfo.InvariantCulture;
            this.FillImpl(dataSet, CommandType.Text, selectSql, commandParameters);
            return dataSet;
        }

        /// <summary>
        /// 返回一个DataSet数据
        /// </summary>
        /// <param name="selectSql">SQL语句</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(string selectSql, params object[] parameterNameAndValues)
        {
            DataSet dataSet = new DataSet();
            dataSet.Locale = CultureInfo.InvariantCulture;
            this.FillImpl(dataSet, CommandType.Text, selectSql, this.NameValueArrayToParamValueArray(parameterNameAndValues));
            return dataSet;
        }

        /// <summary>
        /// 返回一个DataSet数据
        /// </summary>
        /// <param name="spName">SQL存储过程</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public DataSet ExecuteDataSetBySP(string spName, IDataParameter[] commandParameters)
        {
            DataSet dataSet = new DataSet();
            dataSet.Locale = CultureInfo.InvariantCulture;
            this.FillBySP(dataSet, spName, commandParameters);
            return dataSet;
        }

        /// <summary>
        /// 返回一个DataSet数据
        /// </summary>
        /// <param name="spName">SQL存储过程</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public DataSet ExecuteDataSetBySP(string spName, params object[] parameterNameAndValues)
        {
            DataSet dataSet = new DataSet();
            dataSet.Locale = CultureInfo.InvariantCulture;
            this.FillBySP(dataSet, spName, parameterNameAndValues);
            return dataSet;
        }

        /// <summary>
        /// 返回一个DataTable数据
        /// </summary>
        /// <param name="selectSql">SQL语句</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(string selectSql, IDataParameter[] commandParameters)
        {
            DataTable dataTable = new DataTable();
            dataTable.Locale = CultureInfo.InvariantCulture;
            this.Fill(dataTable, selectSql, commandParameters);
            return dataTable;
        }

        /// <summary>
        /// 返回一个DataTable数据
        /// </summary>
        /// <param name="selectSql">SQL语句</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(string selectSql, params object[] parameterNameAndValues)
        {
            DataTable dataTable = new DataTable();
            dataTable.Locale = CultureInfo.InvariantCulture;
            this.Fill(dataTable, selectSql, parameterNameAndValues);
            return dataTable;
        }

        /// <summary>
        /// 返回一个DataTable数据
        /// </summary>
        /// <param name="spName">SQL存储过程</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public DataTable ExecuteDataTableBySP(string spName, IDataParameter[] commandParameters)
        {
            DataTable dataTable = new DataTable();
            dataTable.Locale = CultureInfo.InvariantCulture;
            this.FillBySP(dataTable, spName, commandParameters);
            return dataTable;
        }

        /// <summary>
        /// 返回一个DataTable数据
        /// </summary>
        /// <param name="spName">SQL存储过程</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public DataTable ExecuteDataTableBySP(string spName, params object[] parameterNameAndValues)
        {
            DataTable dataTable = new DataTable();
            dataTable.Locale = CultureInfo.InvariantCulture;
            this.FillBySP(dataTable, spName, parameterNameAndValues);
            return dataTable;
        }

        /// <summary>
        /// 返回执行影响数量
        /// </summary>
        /// <param name="sqlList">批量SQL</param>
        /// <returns></returns>
        public int ExecuteNonQuery(IList<string> sqlList)
        {
            int iTotal = 0;
            foreach (string sql in sqlList)
            {
                iTotal += this.ExecuteNonQueryImpl(CommandType.Text, sql, null);
            }
            return iTotal;
        }
       
        /// <summary>
        /// 返回执行影响数量
        /// </summary>
        /// <param name="nonQuerySql">无查询SQL</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string nonQuerySql, IDataParameter[] commandParameters)
        {
            return this.ExecuteNonQueryImpl(CommandType.Text, nonQuerySql, commandParameters);
        }

        /// <summary>
        /// 返回执行影响数量
        /// </summary>
        /// <param name="nonQuerySql">无查询SQL</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string nonQuerySql, params object[] parameterNameAndValues)
        {
            return this.ExecuteNonQuery(nonQuerySql, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        /// 返回执行影响数量
        /// </summary>
        /// <param name="spName">无查询存储过程</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public int ExecuteNonQueryBySP(string spName, IDataParameter[] commandParameters)
        {
            return this.ExecuteNonQueryImpl(CommandType.StoredProcedure, spName, commandParameters);
        }

        /// <summary>
        /// 返回执行影响数量
        /// </summary>
        /// <param name="spName">无查询存储过程</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public int ExecuteNonQueryBySP(string spName, params object[] parameterNameAndValues)
        {
            return this.ExecuteNonQueryBySP(spName, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        /// 返回Model数据
        /// </summary>
        /// <typeparam name="T">Model类型</typeparam>
        /// <param name="selectSql">查询语句</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public IList<T> ExecuteQuery<T>(string selectSql, IDataParameter[] commandParameters)
        {
            List<T> resultList = new List<T>();
            this.ExecuteQueryImpl(resultList, typeof(T), CommandType.Text, selectSql, commandParameters);
            return resultList;
        }

        /// <summary>
        /// 返回Model数据
        /// </summary>
        /// <typeparam name="T">Model类型</typeparam>
        /// <param name="selectSql">查询语句</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public IList<T> ExecuteQuery<T>(string selectSql, params object[] parameterNameAndValues)
        {
            return this.ExecuteQuery<T>(selectSql, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        /// 返回Model数据
        /// </summary>
        /// <typeparam name="T">Model类型</typeparam>
        /// <param name="selectSql">查询语句</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public IList ExecuteQuery(Type t, string selectSql, IDataParameter[] commandParameters)
        {
            ArrayList resultList = new ArrayList();
            this.ExecuteQueryImpl(resultList, t, CommandType.Text, selectSql, commandParameters);
            return resultList;
        }

        /// <summary>
        /// 返回Model数据
        /// </summary>
        /// <typeparam name="T">Model类型</typeparam>
        /// <param name="selectSql">查询语句</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public IList ExecuteQuery(Type t, string selectSql, params object[] parameterNameAndValues)
        {
            return this.ExecuteQuery(t, selectSql, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        ///  返回Model数据
        /// </summary>
        /// <typeparam name="T">Model类型</typeparam>
        /// <param name="spName">存储过程名</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public IList<T> ExecuteQueryBySP<T>(string spName, IDataParameter[] commandParameters)
        {
            List<T> resultList = new List<T>();
            this.ExecuteQueryImpl(resultList, typeof(T), CommandType.StoredProcedure, spName, commandParameters);
            return resultList;
        }

        /// <summary>
        ///  返回Model数据
        /// </summary>
        /// <typeparam name="T">Model类型</typeparam>
        /// <param name="spName">存储过程名</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public IList<T> ExecuteQueryBySP<T>(string spName, params object[] parameterNameAndValues)
        {
            return this.ExecuteQueryBySP<T>(spName, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        ///  返回Model数据
        /// </summary>
        /// <typeparam name="T">Model类型</typeparam>
        /// <param name="spName">存储过程名</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public IList ExecuteQueryBySP(Type t, string spName, IDataParameter[] commandParameters)
        {
            ArrayList resultList = new ArrayList();
            this.ExecuteQueryImpl(resultList, t, CommandType.StoredProcedure, spName, commandParameters);
            return resultList;
        }

        /// <summary>
        ///  返回Model数据
        /// </summary>
        /// <typeparam name="T">Model类型</typeparam>
        /// <param name="spName">存储过程名</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public IList ExecuteQueryBySP(Type t, string spName, params object[] parameterNameAndValues)
        {
            return this.ExecuteQueryBySP(t, spName, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        /// 返回第一行第一列的值
        /// </summary>
        /// <param name="selectSql">SQL语句</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public object ExecuteScalar(string selectSql, IDataParameter[] commandParameters)
        {
            return this.ExecuteScalarImpl(CommandType.Text, selectSql, commandParameters);
        }

        /// <summary>
        /// 返回第一行第一列的值
        /// </summary>
        /// <param name="selectSql">SQL语句</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public object ExecuteScalar(string selectSql, params object[] parameterNameAndValues)
        {
            return this.ExecuteScalar(selectSql, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        /// 返回第一行第一列的值
        /// </summary>
        /// <param name="spName">存储过程</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public object ExecuteScalarBySP(string spName, IDataParameter[] commandParameters)
        {
            return this.ExecuteScalarImpl(CommandType.StoredProcedure, spName, commandParameters);
        }

        /// <summary>
        /// 返回第一行第一列的值
        /// </summary>
        /// <param name="spName">存储过程</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public object ExecuteScalarBySP(string spName, params object[] parameterNameAndValues)
        {
            return this.ExecuteDataReaderBySP(spName, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        #endregion

        #region 插入数据
        /// <summary>
        /// 使用Model插入数据
        /// </summary>
        /// <param name="dataModelObj">需要插入的数据Model实例</param>
        /// <param name="tableName">表名</param>
        public int Insert(object dataModelObj, string tableName)
        {
            return this.Insert(dataModelObj, tableName, null);
        }

        /// <summary>
        /// 使用Model插入数据
        /// </summary>
        /// <param name="dataModelObj">需要插入的数据Model实例</param>
        /// <param name="tableName">表名</param>
        /// <param name="fields">更新的字段</param>
        public int Insert(object dataModelObj, string tableName, UpdateFields fields)
        {
            if (dataModelObj == null)
            {
                throw new ArgumentNullException("dataModelObj");
            }
            if (dataModelObj is DataRow)
            {
                return this.InsertDataRow((DataRow)dataModelObj, tableName, fields);
            }
            if (dataModelObj is DataTable)
            {
                return this.InsertDataTable((DataTable)dataModelObj, tableName, fields);
            }
            if (dataModelObj is DataSet)
            {
                return this.InsertDataSet((DataSet)dataModelObj, fields);
            }
            if (dataModelObj is DataView)
            {
                return this.InsertDataTable(((DataView)dataModelObj).Table, tableName, fields);
            }
            if (dataModelObj is DataRowView)
            {
                return this.InsertDataRow(((DataRowView)dataModelObj).Row, tableName, fields);
            }
            if (dataModelObj is IList)
            {
                int num = 0;
                IList list = dataModelObj as IList;
                if (list.Count == 0)
                {
                    return 0;
                }
                Type type = null;
                PropertyInfo[] infoArray = null;
                string fullName = null;

                if (this.IsTransaction)
                {
                    this.BeginTransaction();
                }

                try
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null)
                        {
                            if (((list[i] is IList) || (list[i] is DataTable)) || ((list[i] is DataRow) || (list[i] is DataSet)))
                            {
                                num += this.Insert(list[i], tableName, fields);
                            }
                            else
                            {
                                if (type == null)
                                {
                                    type = list[i].GetType();
                                    infoArray = type.GetProperties();
                                    fullName = type.FullName;
                                }
                                else if (list[i].GetType().FullName != fullName)
                                {
                                    type = list[i].GetType();
                                    infoArray = type.GetProperties();
                                    fullName = type.FullName;
                                }
                                num += this.InsertNonDataTable(list[i], type, infoArray, tableName, fields);
                            }
                        }
                    }
                    //if (flag)
                    //{
                    //    //CommitTransaction();
                    //    flag = false;
                    //}
                    return num;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    //try
                    //{
                    //    if (flag)
                    //    {
                    //        this.RollbackTransaction();
                    //        flag = false;
                    //    }
                    //}
                    //catch
                    //{
                    //}
                }
            }

            Type objType = dataModelObj.GetType();
            PropertyInfo[] properties = dataModelObj.GetType().GetProperties();
            return this.InsertNonDataTable(dataModelObj, objType, properties, tableName, fields);
        }

        /// <summary>
        /// 将Row信息插入数据表
        /// </summary>
        /// <param name="insertRow">行数据</param>
        /// <returns></returns>
        public int InsertDataRow(DataRow insertRow)
        {
            return this.InsertDataRow(insertRow, null, null);
        }
        /// <summary>
        /// 将Row数据插入数据表，只更新指定字段
        /// </summary>
        /// <param name="insertRow">行数据</param>
        /// <param name="fields">更新字段部分</param>
        /// <returns></returns>
        public int InsertDataRow(DataRow insertRow, UpdateFields fields)
        {
            return this.InsertDataRow(insertRow, null, fields);
        }
        /// <summary>
        /// 将Row数据插入数据表，并指定表名
        /// </summary>
        /// <param name="insertRow">行数据</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public int InsertDataRow(DataRow insertRow, string tableName)
        {
            return this.InsertDataRow(insertRow, null, null);
        }
        /// <summary>
        /// 将Row数据插入数据表，并指定表名,只更新指定字段
        /// </summary>
        /// <param name="insertRow">行数据</param>
        /// <param name="tableName">表名</param>
        /// <param name="fields">更新字段部分</param>
        /// <returns></returns>
        public int InsertDataRow(DataRow insertRow, string tableName, UpdateFields fields)
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            StringBuilder builder3 = new StringBuilder();
            if (insertRow == null)
            {
                throw new ArgumentNullException("insertRow");
            }
            if (insertRow.Table.Columns.Count == 0)
            {
                return 0;
            }
            int num = 0;
            string sqlValueString = "";
            ArrayList list = null;
            int num2 = 0;
            IDataParameter dataParameterInstance = null;
            DataTable table = insertRow.Table;
            DataRow row = insertRow;
            builder.AppendLine("INSERT INTO " + this.EncodeTableEntityName(string.IsNullOrEmpty(tableName) ? table.TableName : tableName));
            foreach (DataColumn column in table.Columns)
            {
                if (fields != null)
                {
                    if (fields.ContainsField(column.ColumnName))
                    {
                        if (fields.Option != FieldsOptions.ExludeFields)
                        {
                            goto Label_00C6;
                        }
                        continue;
                    }
                    if (fields.Option == FieldsOptions.IncludeFields)
                    {
                        continue;
                    }
                }
            Label_00C6:
                if (builder2.Length == 0)
                {
                    builder2.Append("(");
                }
                else
                {
                    builder2.Append(",");
                }
                builder2.Append(this.EncodeFieldEntityName(column.ColumnName));
                if (builder3.Length == 0)
                {
                    builder3.Append("(");
                }
                else
                {
                    builder3.Append(",");
                }
                sqlValueString = this.GetSqlValueString(row[column.ColumnName]);
                if (sqlValueString != null)
                {
                    builder3.Append(sqlValueString);
                }
                else
                {
                    num2++;
                    builder3.Append(this.ParamPrefixFullString + num2.ToString());
                    if (list == null)
                    {
                        list = new ArrayList();
                    }
                    dataParameterInstance = this.GetDataParameterInstance();
                    dataParameterInstance.ParameterName = this.ParamPrefixFullString + num2.ToString();
                    dataParameterInstance.Value = row[column.ColumnName];
                    list.Add(dataParameterInstance);
                }
            }
            if (builder2.Length == 0)
            {
                return 0;
            }
            builder2.Append(") ");
            builder3.Append(") ");
            builder.AppendLine(" " + builder2.ToString() + " VALUES " + builder3.ToString() + " ");

            if ((list == null) || (list.Count == 0))
            {
                return (num + this.ExecuteNonQuery(builder.ToString(), new object[0]));
            }
            return (num + this.ExecuteNonQuery(builder.ToString(), (IDataParameter[])list.ToArray(typeof(IDataParameter))));
        }
        /// <summary>
        /// 使用DataSet插入表
        /// </summary>
        /// <param name="dataSet">数据源</param>
        /// <returns></returns>
        public int InsertDataSet(DataSet dataSet)
        {
            return this.InsertDataSet(dataSet, null);
        }
        /// <summary>
        /// 使用DataSet插入表
        /// </summary>
        /// <param name="dataSet">数据源</param>
        /// <param name="fields">更新字段</param>
        /// <returns></returns>
        public int InsertDataSet(DataSet dataSet, UpdateFields fields)
        {
            int num2;
            //bool flag = false;
            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }
            if ((dataSet.Tables.Count == 0) || (dataSet.Tables.Count == 0))
            {
                return 0;
            }
            int num = 0;

            if (this.IsTransaction)
            {
                this.BeginTransaction();
            }

            try
            {
                foreach (DataTable table in dataSet.Tables)
                {
                    num += this.InsertDataTable(table, table.TableName, fields);
                }
                //if (flag)
                //{
                //    //CommitTransaction();
                //    flag = false;
                //}
                num2 = num;
            }
            catch
            {
                throw;
            }
            finally
            {
                //try
                //{
                //    if (flag)
                //    {
                //        this.RollbackTransaction();
                //        flag = false;
                //    }
                //}
                //catch
                //{
                //}
            }
            return num2;
        }
        /// <summary>
        /// 使用Datatable插入数据
        /// </summary>
        /// <param name="dataTable">数据源</param>
        /// <returns></returns>
        public int InsertDataTable(DataTable dataTable)
        {
            return this.InsertDataTable(dataTable, null, null);
        }
        /// <summary>
        /// 使用Datatable插入数据
        /// </summary>
        /// <param name="dataTable">数据源</param>
        /// <param name="fields">更新字段</param>
        /// <returns></returns>
        public int InsertDataTable(DataTable dataTable, UpdateFields fields)
        {
            return this.InsertDataTable(dataTable, null, fields);
        }
        /// <summary>
        /// 使用Datatable插入数据
        /// </summary>
        /// <param name="dataTable">数据源</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public int InsertDataTable(DataTable dataTable, string tableName)
        {
            return this.InsertDataTable(dataTable, tableName, null);
        }
        /// <summary>
        /// 使用Datatable插入数据
        /// </summary>
        /// <param name="dataTable">数据源</param>
        /// <param name="tableName">表名</param>
        /// <param name="fields">更新字段</param>
        /// <returns></returns>
        public int InsertDataTable(DataTable dataTable, string tableName, UpdateFields fields)
        {
            int num2;
            //bool flag = false;
            if (dataTable == null)
            {
                throw new ArgumentNullException("dataTable");
            }
            if ((dataTable.Rows.Count == 0) || (dataTable.Columns.Count == 0))
            {
                return 0;
            }
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = dataTable.TableName;
            }
            int num = 0;

            if (this.IsTransaction)
            {
                this.BeginTransaction();//IsolationLevel.ReadCommitted
            }

            try
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    num += this.InsertDataRow(row, dataTable.TableName, fields);
                }
                //if (num == dataTable.Rows.Count)
                //{
                //    //CommitTransaction();
                //    flag = false;
                //}
                num2 = num;
            }
            catch
            {
                throw;
            }
            finally
            {
                //try
                //{
                //    if (flag)
                //    {
                //        this.RollbackTransaction();
                //        flag = false;
                //    }
                //}
                //catch
                //{
                //}
            }
            return num2;
        }

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="obj">需要插入的数据Model实例</param>
        /// <param name="objType">类型</param>
        /// <param name="properties">属性值</param>
        /// <param name="tableName">数据表名</param>
        /// <param name="fields">更新字段名</param>
        /// <returns></returns>
        private int InsertNonDataTable(object obj, Type objType, PropertyInfo[] properties, string tableName, UpdateFields fields)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (objType == null)
            {
                throw new ArgumentNullException("type");
            }
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            if (properties.Length == 0)
            {
                return 0;
            }
            string sqlValueString = "";
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            StringBuilder builder3 = new StringBuilder();
            int num = 0;
            ArrayList list = null;
            int num2 = 0;
            IDataParameter dataParameterInstance = null;
            builder.Append("INSERT INTO " + this.EncodeTableEntityName(tableName));
            foreach (PropertyInfo info in properties)
            {
                if (!info.CanRead)
                {
                    goto Label_01AE;
                }
                if (fields != null)
                {
                    if (fields.ContainsField(info.Name))
                    {
                        if (fields.Option != FieldsOptions.ExludeFields)
                        {
                            goto Label_00D4;
                        }
                        goto Label_01AE;
                    }
                    if (fields.Option == FieldsOptions.IncludeFields)
                    {
                        goto Label_01AE;
                    }
                }
            Label_00D4:
                if (builder2.Length == 0)
                {
                    builder2.Append("(");
                }
                else
                {
                    builder2.Append(",");
                }
                builder2.Append(this.EncodeFieldEntityName(info.Name));
                if (builder3.Length == 0)
                {
                    builder3.Append("(");
                }
                else
                {
                    builder3.Append(",");
                }
                if (info.Name.IndexOf("_SERVER_DATE") > 0)
                    sqlValueString = "GETDATE()";
                else
                    sqlValueString = this.GetSqlValueString(info.GetValue(obj, null), info.PropertyType.ToString());
                if (sqlValueString != null)
                {
                    builder3.Append(sqlValueString);
                }
                else
                {
                    num2++;
                    builder3.Append(this.ParamPrefixFullString + num2.ToString());
                    if (list == null)
                    {
                        list = new ArrayList();
                    }
                    dataParameterInstance = this.GetDataParameterInstance();
                    dataParameterInstance.ParameterName = this.ParamPrefixFullString + num2.ToString();
                    dataParameterInstance.Value = info.GetValue(obj, null);
                    list.Add(dataParameterInstance);
                }
            Label_01AE: ;
            }
            if (builder2.Length == 0)
            {
                return 0;
            }
            builder2.Append(") ");
            builder3.Append(") ");
            builder.Append(" " + builder2.ToString() + " VALUES " + builder3.ToString() + " ");

            if ((list == null) || (list.Count == 0))
            {
                return (num + this.ExecuteNonQuery(builder.ToString(), new object[0]));
            }
            return (num + this.ExecuteNonQuery(builder.ToString(), (IDataParameter[])list.ToArray(typeof(IDataParameter))));
        }
        #endregion

        #region 更新数据
        /// <summary>
        /// 使用Model更新数据
        /// </summary>
        /// <param name="dataModelObj">需要插入的数据Model实例</param>
        /// <param name="tableName">表名</param>
        /// <param name="primaryKeyFields">主键数组</param>
        /// <returns></returns>
        public int Update(object dataModelObj, string tableName, params string[] primaryKeyFields)
        {
            return this.Update(dataModelObj, tableName, null, primaryKeyFields);
        }
        /// <summary>
        /// 使用Model更新数据
        /// </summary>
        /// <param name="dataModelObj">需要插入的数据Model实例</param>
        /// <param name="tableName">表名</param>
        /// <param name="fields">更新字段</param>
        /// <param name="primaryKeyFields">主键数组</param>
        /// <returns></returns>
        public int Update(object dataModelObj, string tableName, UpdateFields fields, params string[] primaryKeyFields)
        {
            if (dataModelObj == null)
            {
                throw new ArgumentNullException("dataModelObj");
            }
            if (dataModelObj is DataRow)
            {
                return this.UpdateDataRow((DataRow)dataModelObj, tableName, fields, primaryKeyFields);
            }
            if (dataModelObj is DataTable)
            {
                return this.UpdateDataTable((DataTable)dataModelObj, tableName, fields, primaryKeyFields);
            }
            if (dataModelObj is DataSet)
            {
                return this.UpdateDataSet((DataSet)dataModelObj, fields);
            }
            if (dataModelObj is DataView)
            {
                return this.UpdateDataTable(((DataView)dataModelObj).Table, tableName, fields, primaryKeyFields);
            }
            if (dataModelObj is DataRowView)
            {
                return this.UpdateDataRow(((DataRowView)dataModelObj).Row, tableName, fields, primaryKeyFields);
            }
            if (dataModelObj is IList)
            {
                int num = 0;
                IList list = dataModelObj as IList;
                if (list.Count == 0)
                {
                    return 0;
                }
                Type type = null;
                PropertyInfo[] infoArray = null;
                string fullName = null;
                //bool flag = true;

                if (!IsTransaction)
                {
                    this.BeginTransaction();
                    //flag = true;
                }

                try
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null)
                        {
                            if (((list[i] is IList) || (list[i] is DataTable)) || ((list[i] is DataRow) || (list[i] is DataSet)))
                            {
                                num += this.Update(list[i], tableName, fields, primaryKeyFields);
                            }
                            else
                            {
                                if (type == null)
                                {
                                    type = list[i].GetType();
                                    infoArray = type.GetProperties();
                                    fullName = type.FullName;
                                }
                                else if (list[i].GetType().FullName != fullName)
                                {
                                    type = list[i].GetType();
                                    infoArray = type.GetProperties();
                                    fullName = type.FullName;
                                }
                                num += this.UpdateNonDataTable(list[i], type, infoArray, tableName, fields, primaryKeyFields);
                            }
                        }
                    }
                    //if (num > 0)
                    //{
                    //    //CommitTransaction();
                    //    flag = false;
                    //}
                    return num;
                }
                catch
                {
                    throw;
                }
                finally
                {
                    //try
                    //{
                    //    if (flag)
                    //    {
                    //        this.RollbackTransaction();
                    //        flag = false;
                    //    }
                    //}
                    //catch
                    //{
                    //}
                }
            }
            Type objType = dataModelObj.GetType();
            PropertyInfo[] properties = dataModelObj.GetType().GetProperties();
            return this.UpdateNonDataTable(dataModelObj, objType, properties, tableName, fields, primaryKeyFields);
        }
        /// <summary>
        /// 更新行信息
        /// </summary>
        /// <param name="updateRow">行数据</param>
        /// <param name="tableName">表名</param>
        /// <param name="updateConditionFields">更新使用条件Key字段</param>
        /// <returns></returns>
        public int UpdateDataRow(DataRow updateRow, string tableName, params string[] updateConditionFields)
        {
            return this.UpdateDataRow(updateRow, tableName, null, updateConditionFields);
        }
        /// <summary>
        /// 更新行信息
        /// </summary>
        /// <param name="updateRow">行数据</param>
        /// <param name="tableName">表名</param>
        /// <param name="fields">更新字段</param>
        /// <param name="updateConditionFields">更新使用条件Key字段</param>
        /// <returns></returns>
        public int UpdateDataRow(DataRow updateRow, string tableName, UpdateFields fields, params string[] updateConditionFields)
        {
            if (updateRow == null)
            {
                throw new ArgumentNullException("updateRow");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = updateRow.Table.TableName;
            }
            string[] strArray = null;
            if ((updateConditionFields == null) || (updateConditionFields.Length <= 0))
            {
                if ((updateRow.Table.PrimaryKey == null) || (updateRow.Table.PrimaryKey.Length == 0))
                {
                    throw new InvalidOperationException("主键不在数据表：" + tableName + "中");
                }
                strArray = new string[updateRow.Table.PrimaryKey.Length];
                for (int i = 0; i < updateRow.Table.PrimaryKey.Length; i++)
                {
                    strArray[i] = updateRow.Table.PrimaryKey[i].ColumnName;
                }
            }
            else
            {
                strArray = updateConditionFields;
            }
            string sqlValueString = "";
            ArrayList list = null;
            int num2 = 0;
            IDataParameter dataParameterInstance = null;
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            int num3 = 0;
            DataTable table = updateRow.Table;
            DataRow row = updateRow;
            builder.AppendLine("UPDATE " + this.EncodeTableEntityName(tableName) + " SET ");
            foreach (DataColumn column in table.Columns)
            {
                if (fields != null)
                {
                    if (fields.ContainsField(column.ColumnName))
                    {
                        if (fields.Option != FieldsOptions.ExludeFields)
                        {
                            goto Label_0145;
                        }
                        continue;
                    }
                    if (fields.Option == FieldsOptions.IncludeFields)
                    {
                        continue;
                    }
                }
            Label_0145:
                builder2.AppendLine((builder2.Length == 0) ? "" : ",");
                sqlValueString = this.GetSqlValueString(row[column.ColumnName]);
                if (sqlValueString != null)
                {
                    builder2.AppendLine(this.EncodeFieldEntityName(column.ColumnName) + " = " + sqlValueString);
                }
                else
                {
                    num2++;
                    builder2.AppendLine(this.EncodeFieldEntityName(column.ColumnName) + " = " + this.ParamPrefixFullString + num2.ToString());
                    if (list == null)
                    {
                        list = new ArrayList();
                    }
                    dataParameterInstance = this.GetDataParameterInstance();
                    dataParameterInstance.ParameterName = this.ParamPrefixFullString + num2.ToString();
                    dataParameterInstance.Value = row[column.ColumnName];
                    list.Add(dataParameterInstance);
                }
            }
            if (builder2.Length == 0)
            {
                return 0;
            }
            builder.Append(builder2);
            builder.AppendLine(" WHERE ");
            builder2.Remove(0, builder2.Length);
            if (row.HasVersion(DataRowVersion.Original))
            {
                foreach (string str2 in strArray)
                {
                    if (builder2.Length > 0)
                    {
                        builder2.AppendLine(" AND ");
                    }
                    sqlValueString = this.GetSqlValueString(row[str2, DataRowVersion.Original]);
                    if (sqlValueString != null)
                    {
                        builder2.AppendLine(this.EncodeFieldEntityName(str2) + " =" + sqlValueString);
                    }
                    else
                    {
                        num2++;
                        builder2.AppendLine(this.EncodeFieldEntityName(str2) + " = " + this.ParamPrefixFullString + num2.ToString());
                        if (list == null)
                        {
                            list = new ArrayList();
                        }
                        dataParameterInstance = this.GetDataParameterInstance();
                        dataParameterInstance.ParameterName = this.ParamPrefixFullString + num2.ToString();
                        dataParameterInstance.Value = row[str2, DataRowVersion.Original];
                        list.Add(dataParameterInstance);
                    }
                }
            }
            else
            {
                foreach (string str3 in strArray)
                {
                    if (builder2.Length > 0)
                    {
                        builder2.AppendLine(" AND ");
                    }
                    sqlValueString = this.GetSqlValueString(row[str3]);
                    if (sqlValueString != null)
                    {
                        builder2.AppendLine(this.EncodeFieldEntityName(str3) + " = " + sqlValueString);
                    }
                    else
                    {
                        num2++;
                        builder2.AppendLine(this.EncodeFieldEntityName(str3) + " = " + this.ParamPrefixFullString + num2.ToString());
                        if (list == null)
                        {
                            list = new ArrayList();
                        }
                        dataParameterInstance = this.GetDataParameterInstance();
                        dataParameterInstance.ParameterName = this.ParamPrefixFullString + num2.ToString();
                        dataParameterInstance.Value = row[str3];
                        list.Add(dataParameterInstance);
                    }
                }
            }
            builder.Append(builder2);
            builder.AppendLine(" ");
            if (builder.Length <= 0)
            {
                return num3;
            }

            if ((list == null) || (list.Count == 0))
            {
                return (num3 + this.ExecuteNonQuery(builder.ToString(), new object[0]));
            }
            return (num3 + this.ExecuteNonQuery(builder.ToString(), (IDataParameter[])list.ToArray(typeof(IDataParameter))));
        }

        /// <summary>
        /// 根据条件更新行信息
        /// </summary>
        /// <param name="updateRow">行信息</param>
        /// <param name="tableName">表名</param>
        /// <param name="conditionSql">更新使用条件SQL语句</param>
        /// <returns></returns>
        public int UpdateDataRowByCondition(DataRow updateRow, string tableName, string conditionSql)
        {
            return this.UpdateDataRowByCondition(updateRow, tableName, null, conditionSql);
        }

        /// <summary>
        /// 根据条件更新行信息
        /// </summary>
        /// <param name="updateRow">行信息</param>
        /// <param name="tableName">表名</param>
        /// <param name="fields">更新字段</param>
        /// <param name="conditionSql">更新使用条件SQL语句</param>
        /// <returns></returns>
        public int UpdateDataRowByCondition(DataRow updateRow, string tableName, UpdateFields fields, string conditionSql)
        {
            if (updateRow == null)
            {
                throw new ArgumentNullException("updateRow");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = updateRow.Table.TableName;
            }
            string sqlValueString = "";
            ArrayList list = null;
            int num = 0;
            IDataParameter dataParameterInstance = null;
            string str2 = "";
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            int num2 = 0;
            DataTable table = updateRow.Table;
            DataRow row = updateRow;
            builder.AppendLine("UPDATE " + this.EncodeTableEntityName(tableName) + " SET ");
            foreach (DataColumn column in table.Columns)
            {
                if (fields != null)
                {
                    if (fields.ContainsField(column.ColumnName))
                    {
                        if (fields.Option != FieldsOptions.ExludeFields)
                        {
                            goto Label_00BC;
                        }
                        continue;
                    }
                    if (fields.Option == FieldsOptions.IncludeFields)
                    {
                        continue;
                    }
                }
            Label_00BC:
                builder2.AppendLine((builder2.Length == 0) ? "" : ",");
                sqlValueString = this.GetSqlValueString(row[column.ColumnName]);
                if (sqlValueString != null)
                {
                    builder2.AppendLine(this.EncodeFieldEntityName(column.ColumnName) + " = " + sqlValueString);
                }
                else
                {
                    num++;
                    builder2.AppendLine(this.EncodeFieldEntityName(column.ColumnName) + " = " + this.ParamPrefixFullString + num.ToString());
                    if (list == null)
                    {
                        list = new ArrayList();
                    }
                    dataParameterInstance = this.GetDataParameterInstance();
                    dataParameterInstance.ParameterName = this.ParamPrefixFullString + num.ToString();
                    dataParameterInstance.Value = row[column.ColumnName];
                    list.Add(dataParameterInstance);
                }
            }
            if (builder2.Length == 0)
            {
                return 0;
            }
            builder.AppendLine(builder2.ToString());
            builder2.Remove(0, builder2.Length);
            if (!string.IsNullOrEmpty(conditionSql))
            {
                builder2.AppendLine(" WHERE " + conditionSql);
            }
            builder.AppendLine(builder2.ToString());
            builder.AppendLine(" ");
            if (str2.Length <= 0)
            {
                return num2;
            }

            if ((list == null) || (list.Count > 0))
            {
                return (num2 + this.ExecuteNonQuery(str2.ToString(), new object[0]));
            }
            return (num2 + this.ExecuteNonQuery(str2.ToString(), (IDataParameter[])list.ToArray(dataParameterInstance.GetType())));
        }

        /// <summary>
        /// 使用DataSet更新数据库
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public int UpdateDataSet(DataSet dataSet)
        {
            return this.UpdateDataSet(dataSet, null);
        }
        /// <summary>
        /// 使用DataSet更新数据库
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="fields">更新字段</param>
        /// <returns></returns>
        public int UpdateDataSet(DataSet dataSet, UpdateFields fields)
        {
            int num2;
            //bool flag = false;
            int num = 0;
            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }
            if (dataSet.Tables.Count == 0)
            {
                return 0;
            }

            if (!IsTransaction)
            {
                this.BeginTransaction();//IsolationLevel.ReadCommitted
                //flag = true;
            }

            try
            {
                foreach (DataTable table in dataSet.Tables)
                {
                    num += this.UpdateDataTable(table, table.TableName, fields, new string[0]);
                }
                //if (num > 0)
                //{
                //    //CommitTransaction();
                //    flag = false;
                //}
                num2 = num;
            }
            catch
            {
                throw;
            }
            finally
            {
                //try
                //{
                //    if (flag)
                //    {
                //        this.RollbackTransaction();
                //        flag = false;
                //    }
                //}
                //catch
                //{
                //}
            }
            return num2;
        }
        /// <summary>
        /// 使用DataTable更新指定条件表
        /// </summary>
        /// <param name="dataTable">数据源</param>
        /// <param name="tableName">表名</param>
        /// <param name="updateConditionFields">条件字段</param>
        /// <returns></returns>
        public int UpdateDataTable(DataTable dataTable, string tableName, params string[] updateConditionFields)
        {
            return this.UpdateDataTable(dataTable, tableName, null, updateConditionFields);
        }
        /// <summary>
        /// 使用DataTable更新指定条件表
        /// </summary>
        /// <param name="dataTable">数据源</param>
        /// <param name="tableName">表名</param>
        /// <param name="fields">更新字段</param>
        /// <param name="updateConditionFields">条件字段</param>
        /// <returns></returns>
        public int UpdateDataTable(DataTable dataTable, string tableName, UpdateFields fields, params string[] updateConditionFields)
        {
            int num2;
            //bool flag = false;
            int num = 0;
            if (dataTable == null)
            {
                throw new ArgumentNullException("dataTable");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = dataTable.TableName;
            }
            if ((dataTable.Rows.Count == 0) || (dataTable.Columns.Count == 0))
            {
                return 0;
            }

            if (!IsTransaction)
            {
                this.BeginTransaction();//IsolationLevel.ReadCommitted
                //flag = true;
            }

            try
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    num += this.UpdateDataRow(row, tableName, fields, updateConditionFields);
                }
                //if (num == dataTable.Rows.Count)
                //{
                //    //CommitTransaction();
                //    flag = false;
                //}
                num2 = num;
            }
            catch
            {
                throw;
            }
            finally
            {
                //try
                //{
                //    if (flag)
                //    {
                //        this.RollbackTransaction();
                //        flag = false;
                //    }
                //}
                //catch
                //{
                //}
            }
            return num2;
        }
        /// <summary>
        /// 使用DataTable更新指定条件表
        /// </summary>
        /// <param name="dataTable">数据源</param>
        /// <param name="tableName">表名</param>
        /// <param name="conditionSql">条件SQL语句</param>
        /// <returns></returns>
        public int UpdateDataTableByCondition(DataTable dataTable, string tableName, string conditionSql)
        {
            return this.UpdateDataTableByCondition(dataTable, tableName, null, conditionSql);
        }
        /// <summary>
        /// 使用DataTable更新指定条件表
        /// </summary>
        /// <param name="dataTable">数据源</param>
        /// <param name="tableName">表名</param>
        /// <param name="fields">更新字段</param>
        /// <param name="conditionSql">条件SQL语句</param>
        /// <returns></returns>
        public int UpdateDataTableByCondition(DataTable dataTable, string tableName, UpdateFields fields, string conditionSql)
        {
            int num2;
            //bool flag = false;
            int num = 0;
            if (dataTable == null)
            {
                throw new ArgumentNullException("dataTable");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = dataTable.TableName;
            }
            if ((dataTable.Rows.Count == 0) || (dataTable.Columns.Count == 0))
            {
                return 0;
            }

            if (!IsTransaction)
            {
                this.BeginTransaction();//IsolationLevel.ReadCommitted
                //flag = true;
            }

            try
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    num += this.UpdateDataRowByCondition(row, tableName, fields, conditionSql);
                }
                //if (num == dataTable.Rows.Count)
                //{
                //    //CommitTransaction();
                //    flag = false;
                //}
                num2 = num;
            }
            catch
            {
                throw;
            }
            finally
            {
                //try
                //{
                //    if (flag)
                //    {
                //        this.RollbackTransaction();
                //        flag = false;
                //    }
                //}
                //catch
                //{
                //}
            }
            return num2;
        }
        /// <summary>
        /// 使用指定类型更新数据表
        /// </summary>
        /// <param name="obj">需要插入的数据Model实例</param>
        /// <param name="objType">Model类型</param>
        /// <param name="properties">属性字段</param>
        /// <param name="tableName">表名</param>
        /// <param name="fields">更新字段</param>
        /// <param name="primaryKeyProperties">主键属性</param>
        /// <returns></returns>
        private int UpdateNonDataTable(object obj, Type objType, PropertyInfo[] properties, string tableName, UpdateFields fields, params string[] primaryKeyProperties)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (objType == null)
            {
                throw new ArgumentNullException("type");
            }
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            if ((primaryKeyProperties == null) || (primaryKeyProperties.Length == 0))
            {
                throw new ArgumentException("参数数组长度不能为0", "primaryKeyProperties");
            }
            if (properties.Length == 0)
            {
                return 0;
            }
            string strA = "";
            ArrayList list = null;
            int num = 0;
            IDataParameter dataParameterInstance = null;
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            int num2 = 0;
            builder.AppendLine("UPDATE " + this.EncodeTableEntityName(tableName) + " SET ");
            foreach (PropertyInfo info in properties)
            {
                if (!info.CanRead)
                {
                    goto Label_01BA;
                }
                if (fields != null)
                {
                    if (fields.ContainsField(info.Name))
                    {
                        if (fields.Option != FieldsOptions.ExludeFields)
                        {
                            goto Label_00F9;
                        }
                        goto Label_01BA;
                    }
                    if (fields.Option == FieldsOptions.IncludeFields)
                    {
                        goto Label_01BA;
                    }
                }
            Label_00F9:
                builder2.AppendLine((builder2.Length == 0) ? "" : ",");
                strA = this.GetSqlValueString(info.GetValue(obj, null), info.PropertyType.ToString());
                if (strA != null)
                {
                    builder2.AppendLine(this.EncodeFieldEntityName(info.Name) + " = " + strA);
                }
                else
                {
                    num++;
                    builder2.AppendLine(this.EncodeFieldEntityName(info.Name) + " = " + this.ParamPrefixFullString + num.ToString());
                    if (list == null)
                    {
                        list = new ArrayList();
                    }
                    dataParameterInstance = this.GetDataParameterInstance();
                    dataParameterInstance.ParameterName = this.ParamPrefixFullString + num.ToString();
                    dataParameterInstance.Value = info.GetValue(obj, null);
                    list.Add(dataParameterInstance);
                }
            Label_01BA: ;
            }
            if (builder2.Length == 0)
            {
                return 0;
            }
            builder.Append(builder2);
            builder.AppendLine(" WHERE ");
            builder2.Remove(0, builder2.Length);
            PropertyInfo info2 = null;
            foreach (string str2 in primaryKeyProperties)
            {
                if (string.IsNullOrEmpty(str2))
                {
                    throw new ArgumentException("数组" + primaryKeyProperties + "长度不能为0", "primaryKeyProperties");
                }
                if (builder2.Length > 0)
                {
                    builder2.AppendLine(" AND ");
                }
                info2 = this.FindProperty(properties, str2);
                if (info2 == null)
                {
                    throw new ArgumentException("指定类型" + str2 + "不在primaryKeyProperties中", "primaryKeyProperties");
                }
                strA = this.GetSqlValueString(info2.GetValue(obj, null), info2.PropertyType.ToString());
                if (strA != null)
                {
                    if (string.Compare(strA, "null", true) == 0)
                    {
                        builder2.AppendLine(this.EncodeFieldEntityName(str2) + " IS " + strA);
                    }
                    else
                    {
                        builder2.AppendLine(this.EncodeFieldEntityName(str2) + " = " + strA);
                    }
                }
                else
                {
                    num++;
                    builder2.AppendLine(this.EncodeFieldEntityName(str2) + " = " + this.ParamPrefixFullString + num.ToString());
                    if (list == null)
                    {
                        list = new ArrayList();
                    }
                    dataParameterInstance = this.GetDataParameterInstance();
                    dataParameterInstance.ParameterName = this.ParamPrefixFullString + num.ToString();
                    dataParameterInstance.Value = info2.GetValue(obj, null);
                    list.Add(dataParameterInstance);
                }
            }
            builder.Append(builder2);
            builder.AppendLine(" ");
            if (builder.Length <= 0)
            {
                return num2;
            }

            if ((list == null) || (list.Count == 0))
            {
                return (num2 + this.ExecuteNonQuery(builder.ToString(), new object[0]));
            }
            return (num2 + this.ExecuteNonQuery(builder.ToString(), (IDataParameter[])list.ToArray(typeof(IDataParameter))));
        }

        #endregion

        #region 查询
        /// <summary>
        ///  将数据取出至DataTable
        /// </summary>
        /// <param name="dataTable">返回的填充的数据信息</param>
        /// <param name="selectSql">执行的SQL语句</param>
        /// <param name="commandParameters">参数信息</param>
        public void Fill(DataTable dataTable, string selectSql, IDataParameter[] commandParameters)
        {
            this.FillImpl(dataTable, CommandType.Text, selectSql, commandParameters);
        }
        /// <summary>
        ///  将数据取出至DataTable
        /// </summary>
        /// <param name="dataTable">返回的填充的数据信息</param>
        /// <param name="selectSql">执行的SQL语句</param>
        /// <param name="parameterNameAndValues">参数信息</param>
        public void Fill(DataTable dataTable, string selectSql, params object[] parameterNameAndValues)
        {
            this.Fill(dataTable, selectSql, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }
        /// <summary>
        /// 将数据取出至DataSet
        /// </summary>
        /// <param name="dataSet">返回的填充的数据信息</param>
        /// <param name="spName">执行的SQL存储过程名称</param>
        /// <param name="parameterNameAndValues">参数信息</param>
        public void FillBySP(DataSet dataSet, string spName, IDataParameter[] commandParameters)
        {
            this.FillImpl(dataSet, CommandType.StoredProcedure, spName, commandParameters);
        }
        /// <summary>
        /// 将数据取出至DataSet
        /// </summary>
        /// <param name="dataSet">返回的填充的数据信息</param>
        /// <param name="spName">执行的SQL存储过程名称</param>
        /// <param name="parameterNameAndValues">参数信息</param>
        public void FillBySP(DataSet dataSet, string spName, params object[] parameterNameAndValues)
        {
            this.FillImpl(dataSet, CommandType.StoredProcedure, spName, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }
        /// <summary>
        ///  将数据取出至DataTable
        /// </summary>
        /// <param name="dataTable">返回的填充的数据信息</param>
        /// <param name="spName">执行的SQL存储过程名称</param>
        /// <param name="commandParameters">参数信息</param>
        public void FillBySP(DataTable dataTable, string spName, IDataParameter[] commandParameters)
        {
            this.FillImpl(dataTable, CommandType.StoredProcedure, spName, commandParameters);
        }
        /// <summary>
        ///  将数据取出至DataTable
        /// </summary>
        /// <param name="dataTable">返回的填充的数据信息</param>
        /// <param name="spName">执行的SQL存储过程名称</param>
        /// <param name="parameterNameAndValues">参数信息</param>
        public void FillBySP(DataTable dataTable, string spName, params object[] parameterNameAndValues)
        {
            this.FillImpl(dataTable, CommandType.StoredProcedure, spName, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }
        /// <summary>
        ///  将数据取出至DataTable
        /// </summary>
        /// <param name="dataSet">返回的填充的数据信息</param>
        /// <param name="commandType">指定的命令类型信息</param>
        /// <param name="commandText">执行的文本参数信息</param>
        /// <param name="commandParameters">参数信息</param>
        public void FillImpl(DataSet dataSet, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException("dataSet");
            }

            SqlCommand selectCommand = null;
            try
            {
                selectCommand = (SqlCommand)this.GetCommand(commandType, commandText, commandParameters);
                new SqlDataAdapter(selectCommand).Fill(dataSet);
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (selectCommand != null)
                    {
                        selectCommand.Parameters.Clear();
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        ///  将数据取出至DataTable
        /// </summary>
        /// <param name="dataTable">返回的填充的数据信息</param>
        /// <param name="commandType">指定的命令类型信息</param>
        /// <param name="commandText">执行的文本参数信息</param>
        /// <param name="commandParameters">参数信息</param>
        private void FillImpl(DataTable dataTable, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (dataTable == null)
            {
                throw new ArgumentNullException("dataTable");
            }

            SqlCommand selectCommand = null;
            try
            {
                selectCommand = (SqlCommand)this.GetCommand(commandType, commandText, commandParameters);
                new SqlDataAdapter(selectCommand).Fill(dataTable);
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (selectCommand != null)
                    {
                        selectCommand.Parameters.Clear();
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 分页检索数据返回
        /// </summary>
        /// <typeparam name="T">返回数据类型</typeparam>
        /// <param name="list">返回的List数据</param>
        /// <param name="selectSql">SQL语句</param>
        /// <param name="pageSize">每页数据量</param>
        /// <param name="curPageNum">当前第几页</param>
        /// <param name="commandParameters">参数</param>
        /// <param name="commandParameters"></param>
        public virtual int FillPaginalData<T>(IList<T> list, string selectSql, int pageSize, int curPageNum, IDataParameter[] commandParameters)
        {
            IDataReader reader = null;
            int num3;
            if (pageSize <= 0)
            {
                throw new ArgumentException("页码必须大于0", "pageSize");
            }
            if (curPageNum <= 0)
            {
                throw new ArgumentException("页码必须大于0", "curPageNum");
            }

            try
            {
                reader = this.ExecuteDataReader(selectSql, commandParameters);
                int startIndex = pageSize * (curPageNum - 1);
                int num2 = (pageSize * curPageNum) - 1;
                num3 = this.DataReaderToList(list as IList, typeof(T), reader, startIndex, new int?(num2));
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                }
                catch
                {
                }
            }
            return num3;
        }
        /// <summary>
        /// 分页检索数据返回
        /// </summary>
        /// <typeparam name="T">返回数据类型</typeparam>
        /// <param name="list">返回的List数据</param>
        /// <param name="selectSql">SQL语句</param>
        /// <param name="pageSize">每页数据量</param>
        /// <param name="curPageNum">当前第几页</param>
        /// <param name="commandParameters">参数</param>
        public int FillPaginalData<T>(IList<T> list, string selectSql, int pageSize, int curPageNum, params object[] parameterNameAndValues)
        {
            return this.FillPaginalData<T>(list, selectSql, pageSize, curPageNum, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        /// 分页检索数据返回
        /// </summary>
        /// <param name="dataTable">返回的DataTable数据</param>
        /// <param name="selectSqlOrTableName">SQL语句</param>
        /// <param name="pageSize">页面记录数量</param>
        /// <param name="curPageNum">当前页码</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public int FillPaginalData(DataTable dataTable, string selectSql, int pageSize, int curPageNum, IDataParameter[] commandParameters)
        {
            IDataReader reader = null;
            int num;
            try
            {
                reader = this.ExecuteDataReader(selectSql, commandParameters);
                num = this.FillDataTable(dataTable, reader, pageSize, curPageNum);
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                }
                catch
                {
                }
            }
            return num;
        }
        /// <summary>
        /// 分页检索数据返回
        /// </summary>
        /// <param name="dataTable">返回的DataTable数据</param>
        /// <param name="selectSqlOrTableName">SQL语句或表名</param>
        /// <param name="sortOrders">排序条件</param>
        /// <param name="pageSize">页面记录数量</param>
        /// <param name="curPageNum">当前页码</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public int FillPaginalData(DataTable dataTable, string selectSql, int pageSize, int curPageNum, params object[] parameterNameAndValues)
        {
            return this.FillPaginalData(dataTable, selectSql, pageSize, curPageNum, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }
        /// <summary>
        /// 分页检索数据返回
        /// </summary>
        /// <param name="dataTable">返回的DataTable数据</param>
        /// <param name="spName">SQL存储过程</param>
        /// <param name="pageSize">页面记录数量</param>
        /// <param name="curPageNum">当前页码</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public int FillPaginalDataBySP<T>(IList<T> list, string spName, int pageSize, int curPageNum, IDataParameter[] commandParameters)
        {
            IDataReader reader = null;
            int num3;
            try
            {
                reader = this.ExecuteDataReaderBySP(spName, commandParameters);
                int startIndex = pageSize * (curPageNum - 1);
                int num2 = (pageSize * curPageNum) - 1;
                num3 = this.DataReaderToList(list as IList, typeof(T), reader, startIndex, new int?(num2));
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                }
                catch
                {
                }
            }
            return num3;
        }
        /// <summary>
        /// 分页检索数据返回
        /// </summary>
        /// <param name="dataTable">返回的List数据</param>
        /// <param name="spName">SQL存储过程</param>
        /// <param name="pageSize">页面记录数量</param>
        /// <param name="curPageNum">当前页码</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public int FillPaginalDataBySP<T>(IList<T> list, string spName, int pageSize, int curPageNum, params object[] parameterNameAndValues)
        {
            return this.FillPaginalDataBySP<T>(list, spName, pageSize, curPageNum, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }
        /// <summary>
        /// 分页检索数据返回
        /// </summary>
        /// <param name="dataTable">返回的DataTable数据</param>
        /// <param name="spName">SQL存储过程</param>
        /// <param name="pageSize">页面记录数量</param>
        /// <param name="curPageNum">当前页码</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        public int FillPaginalDataBySP(DataTable dataTable, string spName, int pageSize, int curPageNum, IDataParameter[] commandParameters)
        {
            IDataReader reader = null;
            int num;
            try
            {
                reader = this.ExecuteDataReaderBySP(spName, commandParameters);
                num = this.FillDataTable(dataTable, reader, pageSize, curPageNum);
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                }
                catch
                {
                }
            }
            return num;
        }
        /// <summary>
        /// 分页检索数据返回
        /// </summary>
        /// <param name="dataTable">返回的DataTable数据</param>
        /// <param name="spName">SQL存储过程</param>
        /// <param name="pageSize">页面记录数量</param>
        /// <param name="curPageNum">当前页码</param>
        /// <param name="parameterNameAndValues">参数</param>
        /// <returns></returns>
        public int FillPaginalDataBySP(DataTable dataTable, string spName, int pageSize, int curPageNum, params object[] parameterNameAndValues)
        {
            return this.FillPaginalDataBySP(dataTable, spName, pageSize, curPageNum, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }
        /// <summary>
        /// 返回查询数据
        /// </summary>
        /// <typeparam name="T">泛类型参数</typeparam>
        /// <param name="list">返回的List数据</param>
        /// <param name="selectSql">查询SQL语句</param>
        /// <param name="commandParameters">参数</param>
        public void FillQuery<T>(IList<T> list, string selectSql, IDataParameter[] commandParameters)
        {
            this.FillQuery(list as IList, typeof(T), selectSql, commandParameters);
        }
        /// <summary>
        /// 返回查询数据
        /// </summary>
        /// <typeparam name="T">泛类型参数</typeparam>
        /// <param name="list">返回的List数据</param>
        /// <param name="selectSql">查询SQL语句</param>
        /// <param name="parameterNameAndValues">参数</param>
        public void FillQuery<T>(IList<T> list, string selectSql, params object[] parameterNameAndValues)
        {
            this.FillQuery(list as IList, typeof(T), selectSql, parameterNameAndValues);
        }
        /// <summary>
        /// 返回查询数据
        /// </summary>
        /// <param name="list">返回的List数据</param>
        /// <typeparam name="T">泛类型参数</typeparam>
        /// <param name="selectSql">查询SQL语句</param>
        /// <param name="commandParameters">参数</param>
        public void FillQuery(IList list, Type t, string selectSql, IDataParameter[] commandParameters)
        {
            this.ExecuteQueryImpl(list, t, CommandType.Text, selectSql, commandParameters);
        }
        /// <summary>
        /// 返回查询数据
        /// </summary>
        /// <param name="list">返回的List数据</param>
        /// <typeparam name="T">泛类型参数</typeparam>
        /// <param name="selectSql">查询SQL语句</param>
        /// <param name="parameterNameAndValues">参数</param>
        public void FillQuery(IList list, Type t, string selectSql, params object[] parameterNameAndValues)
        {
            this.FillQuery(list, t, selectSql, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }
        /// <summary>
        /// 返回查询数据
        /// </summary>
        /// <typeparam name="T">泛类型参数</typeparam>
        /// <param name="list">返回的List数据</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="commandParameters">参数</param>
        public void FillQueryBySP<T>(IList<T> list, string spName, IDataParameter[] commandParameters)
        {
            this.FillQueryBySP(list as IList, typeof(T), spName, commandParameters);
        }
        /// <summary>
        /// 返回查询数据
        /// </summary>
        /// <typeparam name="T">泛类型参数</typeparam>
        /// <param name="list">返回的List数据</param>
        /// <param name="spName">存储过程名称</param>
        /// <param name="parameterNameAndValues">参数</param>
        public void FillQueryBySP<T>(IList<T> list, string spName, params object[] parameterNameAndValues)
        {
            this.FillQueryBySP(list as IList, typeof(T), spName, parameterNameAndValues);
        }
        /// <summary>
        /// 返回查询数据
        /// </summary>
        /// <param name="list">返回的List数据</param>
        /// <typeparam name="T">泛类型参数</typeparam>
        /// <param name="selectSql">查询SQL语句</param>
        /// <param name="commandParameters">参数</param>
        public void FillQueryBySP(IList list, Type t, string spName, IDataParameter[] commandParameters)
        {
            this.ExecuteQueryImpl(list, t, CommandType.StoredProcedure, spName, commandParameters);
        }
        /// <summary>
        /// 返回查询数据
        /// </summary>
        /// <typeparam name="T">泛类型参数</typeparam>
        /// <param name="list">返回的List数据</param>
        /// <param name="selectSql">查询SQL语句</param>
        /// <param name="parameterNameAndValues">参数</param>
        public void FillQueryBySP(IList list, Type t, string spName, params object[] parameterNameAndValues)
        {
            this.FillQueryBySP(list, t, spName, this.NameValueArrayToParamValueArray(parameterNameAndValues));
        }

        /// <summary>
        /// 返回查询数据
        /// </summary>
        /// <param name="dataTable">返回的DataTable</param>
        /// <param name="reader">数据流读取方法记录集</param>
        /// <param name="pageSize">页面记录数</param>
        /// <param name="curPageNum">当前页码</param>
        /// <returns></returns>
        protected int FillDataTable(DataTable dataTable, IDataReader reader, int pageSize, int curPageNum)
        {
            if (dataTable == null)
            {
                throw new ArgumentNullException("dataTable");
            }
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (pageSize <= 0)
            {
                throw new ArgumentException("页面数据条数必须大于0", "pageSize");
            }
            if (curPageNum <= 0)
            {
                throw new ArgumentException("当前页码数必须大于0", "curPageNum");
            }
            bool flag = false;
            if (dataTable.Columns.Count == 0)
            {
                flag = true;
            }
            if (flag)
            {
                DataColumn column = null;
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    column = new DataColumn(reader.GetName(i), reader.GetFieldType(i));
                    dataTable.Columns.Add(column);
                }
            }
            int num2 = (pageSize * (curPageNum - 1)) + 1;
            int num3 = pageSize * curPageNum;
            int num4 = 0;
            dataTable.BeginLoadData();
            if (flag)
            {
                object[] values = new object[dataTable.Columns.Count];
                while (reader.Read())
                {
                    num4++;
                    if ((num4 >= num2) && (num4 <= num3))
                    {
                        reader.GetValues(values);
                        dataTable.LoadDataRow(values, false);
                    }
                }
            }
            else
            {
                DataRow row = null;
                while (reader.Read())
                {
                    num4++;
                    if ((num4 >= num2) && (num4 <= num3))
                    {
                        row = dataTable.NewRow();
                        for (int j = 0; j < reader.FieldCount; j++)
                        {
                            if (dataTable.Columns.Contains(reader.GetName(j)))
                            {
                                row[reader.GetName(j)] = reader.GetValue(j);
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }
            dataTable.EndLoadData();
            return num4;
        }
        /// <summary>
        /// 使用主键填充数据
        /// </summary>
        /// <param name="dataTable">返回的数据表信息</param>
        /// <returns></returns>
        public int FillRowsByPrimaryKeysValue(DataTable dataTable)
        {
            if (dataTable == null)
            {
                throw new ArgumentNullException("dataTable");
            }
            if (dataTable.Rows.Count == 0)
            {
                return 0;
            }
            if ((dataTable.PrimaryKey == null) || (dataTable.PrimaryKey.Length == 0))
            {
                throw new ArgumentException("未定义主键");
            }
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();

            foreach (DataRow row in dataTable.Rows)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine(" OR ");
                }
                if (builder2.Length > 0)
                {
                    builder2.Remove(0, builder2.Length);
                }
                foreach (DataColumn column in dataTable.PrimaryKey)
                {
                    if (builder2.Length > 0)
                    {
                        builder2.AppendLine(" AND ");
                    }
                    builder2.AppendLine(column.ColumnName + "=" + this.GetSqlValueString(row[column]));
                }
                builder.AppendLine("(" + builder2 + ")");
            }
            DataTable table = dataTable.Clone();
            this.Load(table, builder.ToString(), "");

            foreach (DataRow row2 in table.Rows)
            {
                object[] keys = new object[table.PrimaryKey.Length];
                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i] = row2[table.PrimaryKey[i]];
                }
                DataRow row3 = dataTable.Rows.Find(keys);
                if (row3 != null)
                {
                    foreach (DataColumn column2 in dataTable.Columns)
                    {
                        row3[column2] = row2[column2.ColumnName];
                    }
                    continue;
                }
            }
            return table.Rows.Count;
        }
        /// <summary>
        /// 获取表数组
        /// </summary>
        /// <param name="dataSet">数据源</param>
        /// <param name="schemaType">架构映射方法</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public DataTable[] FillTableSchema(DataSet dataSet, SchemaType schemaType, string tableName)
        {
            DataTable[] tableArray;

            IDbDataAdapter adapter = this.CreateDataAdapterInstance();
            try
            {
                adapter.SelectCommand.CommandText = "SELECT * FROM " + this.EncodeTableEntityName(tableName);
                tableArray = adapter.FillSchema(dataSet, schemaType);
            }
            catch
            {
                throw;
            }
            finally
            {
                try
                {
                    adapter.SelectCommand.Dispose();
                }
                catch
                {
                }
            }
            return tableArray;
        }

        #endregion

        #region 删除数据
        /// <summary>
        /// 获取表数组
        /// </summary>
        /// <param name="dataSet">数据源</param>
        /// <param name="schemaType">架构映射方法</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public int Delete(string TableName, params object[] Keys)
        {
            if (string.IsNullOrEmpty(TableName))
            {
                throw new ArgumentNullException("TableName");
            }

            StringBuilder str = new StringBuilder();
            str.AppendLine("DELETE FROM " + TableName + " ");
            str.AppendLine(" WHERE 1=1 ");

            if (Keys == null)
            {
                for (int i = 0; i < Keys.Length; i++)
                {
                    string[] key = (string[])Keys[i];
                    str.AppendLine("And " + key[0] + "='" + key[1] + "'");
                }
            }

            int num = this.ExecuteNonQuery(str.ToString());

            return num;
        }
        #endregion

        #region 真正执行的SQL语句

        /// <summary>
        /// 返回Model数据
        /// </summary>
        /// <param name="resultList"></param>
        /// <param name="t"></param>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <param name="commandParameters"></param>
        private void ExecuteQueryImpl(IList resultList, Type t, CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            if (resultList == null)
            {
                throw new ArgumentNullException("resultList");
            }
            if (t == null)
            {
                throw new ArgumentNullException("t");
            }
            if (string.IsNullOrEmpty(this._mDbConnection.ConnectionString))
            {
                throw new ArgumentNullException("无数据库连接！");
            }
            if (string.IsNullOrEmpty(commandText)) return;

            IDbCommand command = null;
            IDataReader reader = null;
            string Sql = commandText, LastSql = "";
            try
            {
                command = this.GetCommand(commandType, Sql, commandParameters);
                reader = command.ExecuteReader();
                this.DataReaderToList(resultList, t, reader, 0, null);
                this.lastSql = LastSql;
            }
            catch (SqlException ex)
            {
                Exception nex = new Exception(ex.Message + "\r\n" + Sql, ex);
                throw nex;
            }
            finally
            {
                if (reader != null)
                {
                    try
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                    catch
                    {
                    }
                }
                if (command != null)
                {
                    try
                    {
                        command.Parameters.Clear();
                        command.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// 返回执行影响数量,[底层操作层]
        /// </summary>
        /// <param name="commandType">执行类型</param>
        /// <param name="commandText">执行SQL</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        private int ExecuteNonQueryImpl(CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            int num = 0;
            if (string.IsNullOrEmpty(this._mDbConnection.ConnectionString))
            {
                throw new ArgumentNullException("无数据库连接！");
            }
            if (string.IsNullOrEmpty(commandText)) return 0;

            if ((commandText.ToUpper().StartsWith("UPDATE ") || commandText.ToUpper().StartsWith("DELETE "))
                && commandText.ToUpper().IndexOf("WHERE") < 0)
            {
                throw new Exception("数据的更新和删除必须包含Where条件！请检查该T-SQL语句是否正确！");
            }

            IDbCommand command = null;
            if (this.IsTransaction)
            {
                this.BeginTransaction(IsolationLevel.ReadCommitted);//IsolationLevel.ReadCommitted
            }

            try
            {
                //记录最后一次的SQL语句
                command = this.GetCommand(commandType, commandText, commandParameters);
                num = command.ExecuteNonQuery();
                this.lastSql = commandText;
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            finally
            {
                if (command != null)
                {
                    try
                    {
                        command.Parameters.Clear();
                        command.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
            return num;
        }

        /// <summary>
        /// 返回执行影响数量,[底层操作层]
        /// </summary>
        /// <param name="commandType">执行类型</param>
        /// <param name="commandText">执行SQL</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        private int ExecuteNonQueryImplNoLog(CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            int num = 0;
            if (string.IsNullOrEmpty(this._mDbConnection.ConnectionString))
            {
                throw new ArgumentNullException("无数据库连接！");
            }
            if (string.IsNullOrEmpty(commandText)) return 0;

            if ((commandText.ToUpper().StartsWith("UPDATE ") || commandText.ToUpper().StartsWith("DELETE "))
                && commandText.ToUpper().IndexOf("WHERE") < 0)
            {
                throw new Exception("数据的更新和删除必须包含Where条件！请检查该T-SQL语句是否正确！");
            }

            IDbCommand command = null;
            if (this.IsTransaction)
            {
                this.BeginTransaction(IsolationLevel.ReadCommitted);//IsolationLevel.ReadCommitted
            }

            try
            {
                command = this.GetCommand(commandType, commandText, commandParameters);
                num = command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Exception nex = new Exception(ex.Message + "\r\n" + commandText, ex);
                throw nex;
            }
            finally
            {
                if (command != null)
                {
                    try
                    {
                        command.Parameters.Clear();
                        command.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
            return num;
        }

        /// <summary>
        /// 返回第一行第一列的值
        /// </summary>
        /// <param name="commandType">命令类型</param>
        /// <param name="commandText">SQL语句或存储过程</param>
        /// <param name="commandParameters">参数</param>
        /// <returns></returns>
        private object ExecuteScalarImpl(CommandType commandType, string commandText, params IDataParameter[] commandParameters)
        {
            object obj2;
            if (string.IsNullOrEmpty(this._mDbConnection.ConnectionString))
            {
                throw new ArgumentNullException("无数据库连接！");
            }
            if (string.IsNullOrEmpty(commandText)) return 0;

            if ((commandText.ToUpper().StartsWith("UPDATE ") || commandText.ToUpper().StartsWith("DELETE "))
               && commandText.ToUpper().IndexOf("WHERE") < 0)
            {
                throw new Exception("数据的更新和删除必须包含Where条件！请检查该T-SQL语句是否正确！");
            }

            IDbCommand command = null;
            try
            {
                //记录最后一次的SQL语句
                command = this.GetCommand(commandType, commandText, commandParameters);
                obj2 = command.ExecuteScalar();
                this.lastSql = commandText;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                try
                {
                    if (command != null)
                    {
                        command.Parameters.Clear();
                        command.Dispose();
                    }
                }
                catch
                {
                }
            }
            return obj2;
        }

        #endregion

        #region 帮助方法
        /// <summary>
        /// 实现IDispose接口
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 实现IDispose接口
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                try
                {
                    if (this.IsTransaction && (this._dbTransaction != null))
                    {
                        this.RollbackTransaction();
                    }
                }
                catch { }

                try
                {
                    if ((this._mDbConnection != null) && (this.State != ConnectionState.Closed))
                    {
                        this.Close();
                    }
                }
                catch { }

                try
                {
                    if (this._mDbConnection != null)
                    {
                        this._mDbConnection.Close();
                        this._mDbConnection.Dispose();
                    }
                }
                catch { }
            }
            this._disposed = true;
        }

        /// <summary>
        /// 关闭当前连接
        /// </summary>
        public void Close()
        {
            if (this._mDbConnection.State != ConnectionState.Closed)
                this._mDbConnection.Close();
        }

        /// <summary>
        /// 完整前缀代码组装
        /// </summary>
        protected string ParamPrefixFullString
        {
            get
            {
                return (this.ParameterPrefixStringInSql + "p");
            }
        }

        /// <summary>
        /// 获取时间查询语句
        /// </summary>
        /// <param name="date"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        protected string GetDateTimeSqlString(DateTime date, string format)
        {
            //if (this.DataProvider == Inf.DevLib.Data.DataAccess.DataProvider.MicrosoftJetOLEDB)
            //{
            //    return ("#" + date.ToString((format == null) ? @"yyyy\/MM\/dd HH\:mm\:ss" : format) + "#");
            //}
            //if (this.DataProvider == Inf.DevLib.Data.DataAccess.DataProvider.MSDAORA)
            //{
            //    if (string.IsNullOrEmpty(format))
            //    {
            //        return ("TO_DATE('" + date.ToString(@"yyyy\-MM\-dd HH\:mm\:ss") + "','YYYY-MM-DD HH24:MI:SS')");
            //    }
            //    format = format.Replace('h', 'H');
            //    string str = format;
            //    str = format.Replace("mm", "MI").ToUpper().Replace("HH", "H").Replace("H", "HH24");
            //    return ("TO_DATE('" + date.ToString(format) + "','" + str + "')");
            //}
            //if (this.DataProvider == Inf.DevLib.Data.DataAccess.DataProvider.SQLOLEDB)
            //{
            //    return ("'" + date.ToString((format == null) ? @"yyyyMMdd HH\:mm\:ss" : format) + "'");
            //}
            return ("'" + date.ToString(string.IsNullOrEmpty(format) ? @"yyyy-MM-dd HH:mm:ss.fff" : format) + "'");
        }

        /// <summary>
        /// 获取查询语句字符串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string GetSqlValueString(object value)
        {
            return this.GetSqlValueString(value, null);
        }

        /// <summary>
        /// 获取查询语句字符串
        /// </summary>
        /// <param name="value">数据</param>
        /// <param name="dateFormat">类型</param>
        /// <returns></returns>
        public string GetSqlValueString(object value, string dateFormat)
        {
            if (value == null || value == DBNull.Value)
            {
                dateFormat = dateFormat.ToUpper();
                if (dateFormat != null
                    && (dateFormat.IndexOf("DECIMAL") > 0
                    || dateFormat.IndexOf("SHORT") > 0
                    || dateFormat.IndexOf("INT") > 0
                    || dateFormat.IndexOf("DOUBLE") > 0
                    || dateFormat.IndexOf("FLOAT") > 0
                    || dateFormat.IndexOf("BYTE") > 0
                    || dateFormat.IndexOf("LONG") > 0))
                {
                    return "0";
                }
                return "NULL";
            }
            else if (((value is string) || (value is char)) || (value is Guid))
            {
                return ("'" + value.ToString().Replace("'", "''") + "'");
            }
            else if (value is DateTime)
            {
                return this.GetDateTimeSqlString((DateTime)value, null);
            }
            else if (value is bool)
            {
                if ((bool)value)
                {
                    return this.TrueValue.ToString();
                }
                return this.FalseValue.ToString();
            }
            else if ((((value is decimal) || (value is short)) || ((value is int) || (value is long))) || ((((value is double) || (value is byte)) || ((value is sbyte) || (value is float))) || (((value is ushort) || (value is uint)) || (value is ulong))))
            {
                return value.ToString();
            }
            return null;
        }

        /// <summary>
        /// 数据读取至List
        /// </summary>
        /// <param name="resultList">返回List清单</param>
        /// <param name="t">数据类型</param>
        /// <param name="reader">数据集流</param>
        /// <param name="startIndex">开始位置</param>
        /// <param name="endIndex">结束位置</param>
        /// <returns>返回数量</returns>
        protected int DataReaderToList(IList resultList, Type t, IDataReader reader, int startIndex, int? endIndex)
        {
            if (resultList == null)
            {
                throw new ArgumentNullException("resultList");
            }
            if (t == null)
            {
                throw new ArgumentNullException("t");
            }
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", "开始行号必须大于0");
            }
            if ((endIndex.HasValue && endIndex.HasValue) && (endIndex < 0))
            {
                throw new ArgumentOutOfRangeException("endIndex", "结束行号必须大于0");
            }
            string[] strArray = new string[reader.FieldCount];
            for (int i = 0; i < strArray.Length; i++)
            {
                strArray[i] = reader.GetName(i);
            }
            PropertyInfo[] infoArray = new PropertyInfo[strArray.Length];
            PropertyInfo[] properties = t.GetProperties();
            bool[] flagArray = new bool[strArray.Length];
            Type[] typeArray = new Type[strArray.Length];
            foreach (PropertyInfo info in properties)
            {
                for (int j = 0; j < strArray.Length; j++)
                {
                    if (string.Compare(info.Name, strArray[j], true, CultureInfo.InvariantCulture) == 0)
                    {
                        infoArray[j] = info;
                        typeArray[j] = this.GetRealDataType(info.PropertyType);
                        if (!typeArray[j].Equals(reader.GetFieldType(j)))
                        {
                            flagArray[j] = true;
                        }
                        break;
                    }
                }
            }
            int num3 = -1;
            while (reader.Read())
            {
                num3++;
                if ((startIndex <= num3) && ((!endIndex.HasValue || !endIndex.HasValue) || (endIndex.Value >= num3)))
                {
                    object obj2 = Activator.CreateInstance(t);
                    for (int k = 0; k < infoArray.Length; k++)
                    {
                        if ((infoArray[k] != null) && infoArray[k].CanWrite)
                        {
                            try
                            {
                                if (reader.IsDBNull(k))
                                {
                                    infoArray[k].SetValue(obj2, null, null);
                                }
                                else if (flagArray[k])
                                {
                                    infoArray[k].SetValue(obj2, Convert.ChangeType(reader.GetValue(k), typeArray[k]), null);
                                }
                                else
                                {
                                    infoArray[k].SetValue(obj2, reader.GetValue(k), null);
                                }
                            }
                            catch (Exception exception)
                            {
                                throw new InvalidCastException("属性值" + infoArray[k].Name + "错误！", exception);
                            }
                        }
                    }
                    resultList.Add(obj2);
                }
            }
            return (num3 + 1);
        }

        /// <summary>
        /// 读取数据表
        /// </summary>
        /// <param name="dataTable"></param>
        public void Load(DataTable dataTable)
        {
            this.Load(dataTable, "", "");
        }

        /// <summary>
        /// 读取数据表
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="conditionSql">数据条件</param>
        public void Load(DataTable dataTable, string conditionSql)
        {
            this.Load(dataTable, conditionSql, "");
        }

        /// <summary>
        /// 将表转换成model
        /// </summary>
        /// <param name="t"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public IList Load(Type t, string tableName)
        {
            return this.Load(t, tableName, null);
        }

        /// <summary>
        /// 将表进行排序
        /// </summary>
        /// <param name="dataTable">表名</param>
        /// <param name="conditionSql">条件</param>
        /// <param name="sort">排序</param>
        public void Load(DataTable dataTable, string conditionSql, string sort)
        {
            if (dataTable == null)
            {
                throw new ArgumentNullException("dataTable");
            }
            if (dataTable.Columns.Count != 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine(this.GetSelectSql(dataTable));
                if (!string.IsNullOrEmpty(conditionSql))
                {
                    builder.AppendLine(" WHERE " + conditionSql);
                }
                if (!string.IsNullOrEmpty(sort))
                {
                    builder.AppendLine(" ORDER BY " + sort);
                }

                this.Fill(dataTable, builder.ToString(), new object[0]);
            }
        }

        /// <summary>
        /// 将表进行转换成Model排序
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="conditionSql">条件</param>
        /// <param name="sort">排序</param>
        public IList<T> Load<T>(string tableName, string conditionSql, string sort)
        {
            IList<T> list = new List<T>();
            this.Load(list as IList, typeof(T), tableName, conditionSql, sort);
            return list;
        }

        /// <summary>
        /// 将表进行转换成Model
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="conditionSql">条件</param>
        public IList Load(Type t, string tableName, string conditionSql)
        {
            return this.Load(t, tableName, conditionSql, null);
        }

        /// <summary>
        /// 将表进行转换成Model排序
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="conditionSql">条件</param>
        /// <param name="sort">排序</param>
        public IList Load(Type t, string tableName, string conditionSql, string sort)
        {
            IList list = new ArrayList();
            this.Load(list, t, tableName, conditionSql, sort);
            return list;
        }

        /// <summary>
        /// 将表进行转换成Model排序
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="conditionSql">条件</param>
        /// <param name="sort">排序</param>
        private void Load(IList list, Type t, string tableName, string conditionSql, string sort)
        {
            if (t == null)
            {
                throw new ArgumentNullException("t");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            PropertyInfo[] properties = t.GetProperties();
            string commandText = this.GetSelectSql(properties, tableName) + (string.IsNullOrEmpty(conditionSql) ? "" : (" WHERE " + conditionSql)) + (string.IsNullOrEmpty(sort) ? "" : (" ORDER BY " + sort));
            this.ExecuteQueryImpl(list, t, CommandType.Text, commandText, new IDataParameter[0]);
        }

        /// <summary>
        /// 将指定表数据导出到Model
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="tableName">表名</param>
        /// <param name="fieldsAndValues">必须成对出现</param>
        /// <returns></returns>
        public T LoadDataModel<T>(string tableName, params object[] fieldsAndValues) where T : class, new()
        {
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            string str = "";
            if ((fieldsAndValues != null) && (fieldsAndValues.Length > 0))
            {
                string str2 = "";
                object obj2 = null;
                for (int i = 0; i < fieldsAndValues.Length; i += 2)
                {
                    str2 = fieldsAndValues[i] as string;
                    if (string.IsNullOrEmpty(str2) || string.IsNullOrEmpty(str2.Trim()))
                    {
                        throw new ArgumentException("记录在" + i + "行出现异常！", "fieldsAndValues");
                    }
                    if (fieldsAndValues.Length <= (i + 1))
                    {
                        throw new ArgumentException("记录在" + i + "行出现异常！", "fieldsAndValues");
                    }
                    obj2 = fieldsAndValues[i + 1];
                    if (!string.IsNullOrEmpty(str))
                    {
                        str = str + " AND ";
                    }
                    str = str + this.EncodeFieldEntityName(str2) + "=" + this.GetSqlValueString(obj2);
                }
            }
            Type type = typeof(T);
            IList<T> list = new List<T>();
            PropertyInfo[] properties = type.GetProperties();
            string selectSql = this.GetSelectSql(properties, tableName) + (string.IsNullOrEmpty(str) ? "" : (" WHERE " + str));

            this.FillPaginalData<T>(list, selectSql, 1, 1, new object[0]);
            if (list.Count == 0)
            {
                return default(T);
            }
            return list[0];
        }

        /// <summary>
        /// 返回字段名
        /// </summary>
        public string EncodeFieldEntityName(string fieldName)
        {
            return fieldName;
        }

        /// <summary>
        /// 返回表名
        /// </summary>
        public string EncodeTableEntityName(string tableName)
        {
            return tableName;
        }

        /// <summary>
        /// 将名值对转换成IDataParameter
        /// </summary>
        /// <param name="parameterNameAndValues"></param>
        /// <returns></returns>
        public IDataParameter[] NameValueArrayToParamValueArray(params object[] parameterNameAndValues)
        {
            if ((parameterNameAndValues == null) && (parameterNameAndValues.Length <= 0))
            {
                return null;
            }
            if ((parameterNameAndValues.Length % 2) != 0)
            {
                throw new ArgumentException("必须是数组");
            }
            IDataParameter[] parameterArray = new IDataParameter[parameterNameAndValues.Length / 2];
            for (int i = 0; i < parameterNameAndValues.Length; i += 2)
            {
                if (!(parameterNameAndValues[i] is string))
                {
                    throw new ArgumentException("参数必须是字符串");
                }
                parameterArray[i / 2] = this.CreateParameter((string)parameterNameAndValues[i], parameterNameAndValues[i + 1]);
            }
            return parameterArray;
        }

        /// <summary>
        /// 返回SqlDataAdapter对象
        /// </summary>
        public IDbDataAdapter CreateDataAdapterInstance()
        {
            return new SqlDataAdapter((SqlCommand)this.CreateCommandInstance());
        }

        /// <summary>
        /// 返回实现了IDbCommand的SqlCommand对象
        /// </summary>
        public IDbCommand CreateCommandInstance()
        {
            IDbCommand command = new SqlCommand();
            command.Connection = this._mDbConnection;
            if (command.Connection.State != ConnectionState.Open) command.Connection.Open();

            if (this._dBCommandTimeOut >= 0)
                command.CommandTimeout = this._dBCommandTimeOut;

            if (this.TransactionInstance != null)
            {
                command.Transaction = (SqlTransaction)this.TransactionInstance;
            }
            return command;
        }

        /// <summary>
        /// 创建参数列表
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns>返回Command对象参数</returns>
        private IDataParameter CreateParameter(string name, object value)
        {
            IDataParameter dataParameterInstance = this.GetDataParameterInstance();
            dataParameterInstance.Value = value;
            dataParameterInstance.ParameterName = name;
            return dataParameterInstance;
        }

        /// <summary>
        /// 返回实现IDataParameter对象
        /// </summary>
        public IDataParameter GetDataParameterInstance()
        {
            return new SqlParameter();
        }

        /// <summary>
        /// 返回实现IDataParameter对象
        /// </summary>
        public IDataParameter GetDataParameterInstance(string paramName, string paramValue, ParameterDirection direction)
        {
            IDataParameter dataParameterInstance = this.GetDataParameterInstance(paramName);
            dataParameterInstance.Value = paramValue;
            dataParameterInstance.Direction = direction;
            return dataParameterInstance;
        }

        /// <summary>
        /// 返回实现IDataParameter对象
        /// </summary>
        public virtual IDataParameter GetDataParameterInstance(string paramName)
        {
            IDataParameter dataParameterInstance = this.GetDataParameterInstance();
            dataParameterInstance.ParameterName = paramName;
            return dataParameterInstance;
        }

        /// <summary>
        /// 返回实现了IDbCommand的SqlCommand对象
        /// </summary>
        protected IDbCommand GetCommand(CommandType commandType, string commandText, params IDataParameter[] parameters)
        {
            IDbCommand command2;
            IDbCommand command = this.CreateCommandInstance();
            try
            {
                command.CommandType = commandType;
                command.CommandText = commandText;
                if ((parameters != null) && (parameters.Length > 0))
                {
                    foreach (IDataParameter parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                }
                command2 = command;
            }
            catch
            {
                try
                {
                    if (command != null)
                    {
                        command.Parameters.Clear();
                        command.Dispose();
                    }
                }
                catch
                {
                }
                throw;
            }
            return command2;
        }

        /// <summary>
        /// 返回数据表查询语句信息
        /// </summary>
        private string GetSelectSql(DataTable dt)
        {
            return this.GetSelectSql(dt, null);
        }

        /// <summary>
        /// 返回数据表查询语句信息
        /// </summary>
        private string GetSelectSql(DataTable dt, string tableName)
        {
            StringBuilder builder = new StringBuilder();
            if (dt == null)
            {
                throw new ArgumentNullException("dt");
            }
            if (dt.Columns.Count == 0)
            {
                throw new ArgumentException("表不含有任何列信息", "dt");
            }
            foreach (DataColumn column in dt.Columns)
            {
                builder.AppendLine(((builder.Length > 0) ? "," : "") + this.EncodeFieldEntityName(column.ColumnName));
            }
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = dt.TableName;
            }
            return ("SELECT " + builder.ToString() + " FROM " + this.EncodeTableEntityName(tableName));
        }

        /// <summary>
        /// 返回数据表查询语句信息
        /// </summary>
        /// <param name="properties">属性信息</param>
        /// <param name="tableName">表名</param>
        private string GetSelectSql(PropertyInfo[] properties, string tableName)
        {
            StringBuilder builder = new StringBuilder();
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException("tableName");
            }
            foreach (PropertyInfo info in properties)
            {
                if (info.CanWrite)
                {
                    builder.AppendLine(((builder.Length > 0) ? "," : "") + this.EncodeFieldEntityName(info.Name));
                }
            }
            return ("SELECT " + builder.ToString() + " FROM " + this.EncodeTableEntityName(tableName));
        }

        /// <summary>
        /// 返回类型值
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private Type GetRealDataType(Type t)
        {
            if (t.IsGenericType && (t.FullName.IndexOf("System.Nullable`") == 0))
            {
                NullableConverter converter = new NullableConverter(t);
                return converter.UnderlyingType;
            }
            return t;
        }

        /// <summary>
        /// 寻找指定属性名数据集
        /// </summary>
        private PropertyInfo FindProperty(PropertyInfo[] properties, string findName)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }
            if (findName == null)
            {
                throw new ArgumentNullException("findName");
            }
            foreach (PropertyInfo info in properties)
            {
                if (string.Compare(info.Name, findName, true, CultureInfo.InvariantCulture) == 0)
                {
                    return info;
                }
            }
            return null;
        }

        #endregion

        #endregion
    }
}
