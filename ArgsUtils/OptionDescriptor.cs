using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgsUtils
{
    public class OptionDescriptor<T> : OptionDescriptorBase<T>
    {
        #region Members

        protected Action<T, bool> _action = null;

        #endregion

        #region Constructors

        public OptionDescriptor(string shortKey, string longKey, string description, Action<T, bool> action)
            : base(shortKey, longKey, description)
        {
            this._action = action;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format("\t {0}, {1} | {2}",
                this._shortKey, this._longKey, this._description);
        }

        public override bool Apply(T options, string optionValue)
        {
            if ((optionValue != this._shortKey) &&
                (optionValue != this._longKey))
            {
                return false;
            }
            this._action(options, true);
            return true;
        }

        #endregion
    }

    public class OptionDescriptor<T, T1> : OptionDescriptorBase<T>
    {
        #region Members

        protected string _parameterName = string.Empty;
        protected Action<T, T1> _action = null;
        protected Action<string, T1> _validator = null;

        #endregion

        #region Constructors

        public OptionDescriptor(string shortKey, string longKey, string parameterName, string description,
            Action<T, T1> action, Action<string, T1> validator)
            : base(shortKey, longKey, description)
        {
            this._parameterName = parameterName;
            this._action = action;
            this._validator = validator;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format("\t {0}<{3}>, {1}<{3}> | {2}",
                this._shortKey, this._longKey, this._description, this._parameterName);
        }

        public override bool Apply(T options, string optionValue)
        {
            if (optionValue.StartsWith(this._shortKey))
            {
                this.SetOption(options, optionValue.Substring(this._shortKey.Length));
                return true;
            }
            if (optionValue.StartsWith(this._longKey))
            {
                this.SetOption(options, optionValue.Substring(this._longKey.Length));
                return true;
            }
            return false;
        }

        #endregion

        #region Protected Methods

        protected void SetOption(T options, string optionValue)
        {
            T1 value = (T1)Convert.ChangeType(optionValue, typeof(T1));
            this._validator(this._parameterName, value);
            this._action(options, value);
        }

        #endregion
    }
}
