using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgsUtils
{
    public class OptionValidators
    {
        #region Public Methods

        public static void ValidateGraterThanZero(string parameterName, int number)
        {
            if (0 >= number)
            {
                throw new ArgumentException(string.Format("{0} must be greater than zero ({1}).",
                    parameterName, number));
            }
        }

        public static void ValidateNothing<T>(string parameterName, T value)
        {
        }

        public static void ValidateContains<T>(string parameterName, T value, IEnumerable<T> values)
        {
            if (values.All(v => false == v.Equals(value)))
            {
                throw new ArgumentException(string.Format("{0} {1} must be one of ({2}).",
                    parameterName, value, string.Join(", ", values)));
            }
        }

        public static void ValidatePath(string parameterName, string path)
        {
            try
            {
                new System.IO.FileInfo(path);
            }
            catch
            {
                throw new ArgumentException(string.Format("{0} must be a valid path ({1}).",
                    parameterName, path));
            }
        }

        public static void ValidateNotNullOrEmpty(string parameterName, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(string.Format("{0} must not null or empty.",
                    parameterName));
            }
        }

        #endregion
    }
}
