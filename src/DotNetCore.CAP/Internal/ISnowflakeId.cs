// Copyright 2010-2012 Twitter, Inc.
// An object that generates IDs. This is broken into a separate class in case we ever want to support multiple worker threads per process

namespace DotNetCore.CAP.Internal;

public interface ISnowflakeId
{
    long NextId();
}