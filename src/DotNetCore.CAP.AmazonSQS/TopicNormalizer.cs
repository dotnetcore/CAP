// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.AmazonSQS
{
    internal static class TopicNormalizer
    {
        public static string NormalizeForAws(this string origin)
        {
            if (origin.Length > 256)
            {
                throw new ArgumentOutOfRangeException(nameof(origin) + " character string length must between 1~256!");
            }
            return origin.Replace(".", "-").Replace(":", "_");
        } 
    }
}
