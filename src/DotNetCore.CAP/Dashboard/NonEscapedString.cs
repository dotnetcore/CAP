// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Dashboard
{
    public class NonEscapedString
    {
        private readonly string _value;

        public NonEscapedString(string value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return _value;
        }
    }
}