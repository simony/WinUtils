using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgsUtils
{
    public class OptionDescriptorSet<T> : List<OptionDescriptorBase<T>>
    {
        #region Public Methods

        public OptionDescriptorSet<T> Add(string shortKey, string longKey, string description, Action<T, bool> action)
        {
            this.Add(new OptionDescriptor<T>(shortKey, longKey, description, action));
            return this;
        }

        public OptionDescriptorSet<T> Add(string shortKey, string longKey, string parameterName, string description,
            Action<T, int> action, Action<string, int> validator)
        {
            this.Add(new OptionDescriptor<T, int>(shortKey, longKey, parameterName, description, action, validator));
            return this;
        }

        public OptionDescriptorSet<T> Add(string shortKey, string longKey, string parameterName, string description,
            Action<T, int> action)
        {
            return this.Add(shortKey, longKey, parameterName, description, action, OptionValidators.ValidateNothing);
        }

        public OptionDescriptorSet<T> Add(string shortKey, string longKey, string parameterName, string description,
            Action<T, string> action, Action<string, string> validator)
        {
            this.Add(new OptionDescriptor<T, string>(shortKey, longKey, parameterName, description, action, validator));
            return this;
        }

        public OptionDescriptorSet<T> Add(string shortKey, string longKey, string parameterName, string description,
            Action<T, string> action)
        {
            return this.Add(shortKey, longKey, parameterName, description, action, OptionValidators.ValidateNothing);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var optionDescriptor in this)
            {
                builder.AppendLine(optionDescriptor.ToString());
            }
            return builder.ToString();
        }

        public bool Apply(T options, string optionValue)
        {
            foreach (var optionDescriptor in this)
            {
                if (optionDescriptor.Apply(options, optionValue))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}
