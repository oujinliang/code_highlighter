//---------------------------------------------------------------------
// <copyright file="Expect.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
// Use of this source code is subject to the terms of the Microsoft 
// end-user license agreement (EULA) under which you licensed this
// SOFTWARE PRODUCT. If you did not accept the terms of the EULA, you 
// are not authorized to use this source code. For a copy of the EULA, 
// please see the LICENSE.RTF on your install media.
// </summary>
//---------------------------------------------------------------------

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
