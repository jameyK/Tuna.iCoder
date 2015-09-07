using System;
using System.Collections.Generic;
using System.Data;

namespace Tuna.DaoAccess
{
    /// <summary>
    /// 数据库操作事务类，用于数据的提交回滚操作
    /// 
    /// 1、修正了Complete()，目前支持多数据库保存数据回滚！
    /// 
    /// 2、修正了在执行提交和混滚中使用认证，回滚所有对应的事务信息
    /// </summary>
    public class DaoTransactionScope : IDisposable
    {
        /// <summary>
        /// 当前事务信息
        /// </summary>
        [ThreadStatic]
        private static DaoTransactionScope _current;

        /// <summary>
        /// 事务集合
        /// </summary>
        [ThreadStatic]
        private static List<DaoTransactionScope> _daoTransactionScopeList;

        /// <summary>
        /// 同一事务操作标记
        /// </summary>
        public string CurrentFlag;

        /// <summary>
        /// 数据连结信息
        /// </summary>
        private DataAccessClient _dataAccessInstance;

        /// <summary>
        /// 数据连接类型
        /// </summary>
        private Type _dataAccessType;

        /// <summary>
        /// 当前是否成功提交
        /// </summary>
        private bool _isComplete;

        /// <summary>
        /// 是否已回收信息
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// 是否为回滚中
        /// </summary>
        private bool _isTransacting;

        /// <summary>
        /// 是否成功取消提交
        /// </summary>
        [ThreadStatic]
        private bool _isUnComplete;

        /// <summary>
        /// 事务所有者
        /// </summary>
        [ThreadStatic]
        private static DaoTransactionScope _transactionOwner;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DaoTransactionScope()
            : this(false)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        ~DaoTransactionScope()
        {
            if (_dataAccessInstance != null)
                _dataAccessInstance.lastSql = null;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="isRequiredTransaction">是否开启事务判断</param>
        public DaoTransactionScope(bool isRequiredTransaction)
        {
            //构建事务标记
            if (CurrentFlag == null || CurrentFlag == "")
                CurrentFlag = new Random().Next(DateTime.Now.Millisecond).ToString();

            //构建事务信息
            if ((_daoTransactionScopeList == null) || (_daoTransactionScopeList.Count == 0))
            {
                if (_daoTransactionScopeList == null)
                {
                    _daoTransactionScopeList = new List<DaoTransactionScope>();
                }
                _isTransacting = isRequiredTransaction;
                _daoTransactionScopeList.Add(this);
                _current = this;
                if (isRequiredTransaction)
                {
                    _transactionOwner = this;
                }
                else
                {
                    _transactionOwner = null;
                }
                _dataAccessInstance = null;
            }
            else
            {
                //if (_current == null)
                //{
                //    throw new NullReferenceException("当前的 DaoTransactionScope 对象（DaoTransactionScope.Current 属性值）已不存在。请在后面调用上一层 DaoTransactionScope 对象的 Complete() or Dispose() 方法。");
                //}
                if (isRequiredTransaction && !_isTransacting)
                {
                    _isTransacting = isRequiredTransaction;
                }
                _daoTransactionScopeList.Add(this);
                _current = this;
                if ((_transactionOwner == null) && isRequiredTransaction)
                {
                    _transactionOwner = this;
                }
            }
        }

        /// <summary>
        /// 实现数据提交信息
        /// </summary>
        public void Complete()
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException("DaoTransactionScope");
            }
            if (_current == null)
            {
                throw new NullReferenceException("当前的 DaoTransactionScope 对象（DaoTransactionScope.Current 属性值）已不存在。请不要重复调用 Complete() 方法。");
            }


            string currentFlag = "";
            for (int i = 0; i < DaoTransactionScopeList.Count; i++)
            {
                if (currentFlag == "")
                    currentFlag = DaoTransactionScopeList[i].CurrentFlag;

                if (DaoTransactionScopeList[i].IsTransacting && DaoTransactionScopeList[i].CurrentFlag == currentFlag)
                {
                    _dataAccessInstance = DaoTransactionScopeList[i].DataAccessInstance;
                    if ((_dataAccessInstance != null) && _dataAccessInstance.IsTransaction)
                    {
                        _dataAccessInstance.CommitTransaction();
                    }
                    //DaoTransactionScopeList.Remove(DaoTransactionScopeList[i]);
                    DaoTransactionScopeList[i].IsTransacting = false;
                    try
                    {
                        _dataAccessInstance.Dispose();
                        _dataAccessInstance = null;
                    }
                    catch { }
                    _transactionOwner = null;
                }
            }

            for (int i = DaoTransactionScopeList.Count - 1; i >= 0; i--)
            {
                if (DaoTransactionScopeList[i].IsTransacting == false)
                    DaoTransactionScopeList.Remove(DaoTransactionScopeList[i]);
            }

            this._isComplete = true;
            _current = null;
        }

        /// <summary>
        /// 数据回收操作
        /// </summary>
        public void Dispose()
        {
            if (!this._isDisposed && _daoTransactionScopeList.Count > 0)
            {
                if (!this._isComplete)
                {
                    this._isUnComplete = true;
                }

                string currentFlag = "";
                for (int i = 0; i < _daoTransactionScopeList.Count; i++)
                {
                    //if (DaoTransactionScopeList[i] == this)
                    //{
                    if (currentFlag == "")
                        currentFlag = DaoTransactionScopeList[i].CurrentFlag;

                    _dataAccessInstance = _daoTransactionScopeList[i].DataAccessInstance;
                    if (_dataAccessInstance != null && !_dataAccessInstance.Disposed && DaoTransactionScopeList[i].CurrentFlag == currentFlag)
                    {
                        if ((_dataAccessInstance.State != ConnectionState.Closed) && _dataAccessInstance.IsTransaction)
                        {
                            try
                            {
                                _dataAccessInstance.RollbackTransaction();
                                //清理失败的SQL语句
                                _dataAccessInstance.lastSql = null;
                            }
                            catch (Exception exception3)
                            {
                                //清理失败的SQL语句
                                _dataAccessInstance.lastSql = null;
                            }
                            finally
                            {
                                try
                                {
                                    _dataAccessInstance.Dispose();
                                    _dataAccessInstance = null;
                                }
                                catch { }
                            }
                        }


                    }
                    _daoTransactionScopeList[i].IsTransacting = false;
                }
                if (this == _transactionOwner)
                {
                    _transactionOwner = null;
                }
                for (int i = _daoTransactionScopeList.Count - 1; i >= 0; i--)
                {
                    if (_daoTransactionScopeList[i].IsTransacting == false)
                        _daoTransactionScopeList.Remove(DaoTransactionScopeList[i]);
                }

                if (_daoTransactionScopeList.Count > 0)
                {
                    _current = _daoTransactionScopeList[_daoTransactionScopeList.Count - 1];
                }
                else
                {
                    _current = null;
                }
                this._isDisposed = true;
            }
        }

        /// <summary>
        /// 当前事物实例
        /// </summary>
        public static DaoTransactionScope Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// 当前事务List清单信息
        /// </summary>
        public static List<DaoTransactionScope> DaoTransactionScopeList
        {
            get
            {
                if (_daoTransactionScopeList == null)
                {
                    _daoTransactionScopeList = new List<DaoTransactionScope>();
                }
                return _daoTransactionScopeList;
            }
        }

        /// <summary>
        /// 当前数据库连接实例
        /// </summary>
        public DataAccessClient DataAccessInstance
        {
            get
            {
                return _dataAccessInstance;
            }
            set
            {
                _dataAccessInstance = value;
                _dataAccessType = value.GetType();
            }
        }

        /// <summary>
        /// 是否进行销毁判断
        /// </summary>
        public bool Disposed
        {
            get
            {
                return this._isDisposed;
            }
        }

        /// <summary>
        /// 是否进行事务处理判断
        /// </summary>
        public bool IsTransacting
        {
            get
            {
                return this._isTransacting;
            }
            set
            {
                this._isTransacting = value;
            }
        }
    }
}
