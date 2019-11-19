// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Messages;
using Newtonsoft.Json;

namespace DotNetCore.CAP.Serialization
{
    public class StringSerializer
    {
        public static string Serialize(Message message)
        {
            return JsonConvert.SerializeObject(message);
        }

        public static Message DeSerialize(string json)
        {
            return JsonConvert.DeserializeObject<Message>(json);
        }
    }
}