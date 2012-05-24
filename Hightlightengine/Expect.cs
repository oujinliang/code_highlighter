/* Copyright (C) 2012  Jinliang Ou */

namespace Org.Jinou.HighlightEngine
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Helper class to check argument, condition...
    /// </summary>
    public static class Expect
    {
        /// <summary>
        /// If the argument is null, throw <see cref="ArgumentNullException"/>
        /// </summary>
        /// <param name="parameter">Object to check.</param>
        /// <param name="parameterName">Parameter name.</param>
        public static void ArgumentNotNull(object parameter, string parameterName)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// If the condition value is false, throw <see cref="ArgumentException"/>
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message"></param>
        public static void ArgumentCheck(bool condition, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message);
            }
        }
    }
}
