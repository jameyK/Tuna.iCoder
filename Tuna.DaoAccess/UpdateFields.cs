using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tuna.DaoAccess
{

    /// <summary>
    /// 用于更新或排除更新的字段
    /// </summary>
    public class UpdateFields
    {
        private List<string> _fields;
        private Dictionary<string, string> _hashFields;
        private FieldsOptions _option;

        /// <summary>
        /// 用于更新使用的字段数据
        /// </summary>
        /// <param name="option">更新模式，排除还是只包含</param>
        /// <param name="fields">字段数组</param>
        public UpdateFields(FieldsOptions option, params string[] fields)
        {
            this._option = option;
            this._fields = new List<string>();
            this._hashFields = new Dictionary<string, string>();
            string str = null;
            if (fields != null)
            {
                foreach (string str2 in fields)
                {
                    if (!string.IsNullOrEmpty(str2) && !string.IsNullOrEmpty(str2.Trim()))
                    {
                        str = str2.Trim();
                        if (!this._hashFields.ContainsKey(str.ToUpper()))
                        {
                            this._hashFields.Add(str.ToUpper(), str);
                            this._fields.Add(str);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断是否包含指定字段
        /// </summary>
        /// <param name="field"></param>
        public bool ContainsField(string field)
        {
            return ((!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(field.Trim())) && this._hashFields.ContainsKey(field.ToUpper()));
        }

        public string[] Fields
        {
            get
            {
                return this._fields.ToArray();
            }
        }

        /// <summary>
        /// 指定字段类型
        /// </summary>
        public FieldsOptions Option
        {
            get
            {
                return this._option;
            }
            set
            {
                this._option = value;
            }
        }

    }
 
    /// <summary>
    /// 指定更新字段选项
    /// </summary>
    [Serializable, Description("指定更新字段选项。")]
    public enum FieldsOptions
    {
        [Description("排除指定的字段。")]
        ExludeFields = 1,
        [Description("包含指定的字段。")]
        IncludeFields = 0
    }
}
