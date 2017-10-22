using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Abstractions
{
    public interface IContentSerializer
    {
        string Serialize<T>(T obj);

        T DeSerialize<T>(string content);

        object DeSerialize(string content, Type type);
    }

    public interface IMessagePacker
    {
        string Pack(CapMessage obj);

        CapMessage UnPack(string packingMessage);
    }
}