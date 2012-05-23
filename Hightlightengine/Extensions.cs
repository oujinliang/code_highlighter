﻿//---------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Microsoft">
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

namespace Microsoft.Internals.Tools.Ding.HighlightEngine
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// C# 3.0 Extension Methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Binary Search.
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="list">A list to search</param>
        /// <param name="comparer">Delegate to compare the element.</param>
        /// <returns>Search result index.</returns>
        public static int BinarySearch<T>(this IList<T> list, Func<T, int> comparer)
        {
            int min = 0;
            int max = list.Count - 1;
            while (min <= max)
            {
                int mid = min + ((max - min) >> 1);
                int result = comparer(list[mid]);
                if (result == 0)
                {
                    return mid;
                }
                if (result < 0)
                {
                    min = mid + 1;
                }
                else
                {
                    max = mid - 1;
                }
            }
            return ~min;
        }
    }
}
