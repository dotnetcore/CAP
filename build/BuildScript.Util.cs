﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BuildScript
{
    public partial class BuildScript
    {
        public string CreateStamp()
        {
            var seconds = (long)(DateTime.UtcNow - new DateTime(2017, 1, 1)).TotalSeconds;
            return seconds.ToString();
        }
    }
}
