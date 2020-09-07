// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Persistence;
using Newtonsoft.Json;
using System;

namespace DotNetCore.CAP.LiteDB
{
    public class LiteDBMessage 
    {
        MediumMessage _message;
         public LiteDBMessage(  MediumMessage  message)
        {
            _message = message;
        }
        public LiteDBMessage(Message Origin):this()
        {
            _message.Origin= Origin;
        }
        public LiteDBMessage()
        {
            _message = new MediumMessage();
            _message.Origin = null;
        }
        
        public string Id
        {
            get { return _message.DbId; }
            set { _message.DbId = value; }
        }
        public string Name { get; set; }
       
        public StatusName StatusName { get; set; }

        public string Content
        {
            get { return _message.Content; }
            set { _message.Content = value; }
        }
      
        public int  Retries
        {
            get { return _message.Retries; }
            set { _message.Retries = value; }
        }

        public DateTime Added
        {
            get { return _message.Added; }
            set { _message.Added = value; }
        }
        public DateTime? ExpiresAt
        {
            get { return _message.ExpiresAt; }
            set { _message.ExpiresAt = value; }
        }
        public string Group { get; set; }

        public static explicit operator MediumMessage(CAP.LiteDB.LiteDBMessage v)
        {
            return v._message;
        }
    }
}
