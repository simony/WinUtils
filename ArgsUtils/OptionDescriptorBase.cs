using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgsUtils
{
    public abstract class OptionDescriptorBase<T>
    {
        #region Members

        protected string _shortKey = string.Empty;
        protected string _longKey = string.Empty;
        protected string _description = string.Empty;

        #endregion

        #region Constructors

        public OptionDescriptorBase(string shortKey, string longKey, string description)
        {
            this._shortKey = shortKey;
            this._longKey = longKey;
            this._description = description;
        }

        #endregion

        #region Public Methods

        public abstract bool TryApply(T options, string optionValue);

        #endregion
    }
}
