using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Abstractions
{
    public interface IContentSerializer
    {
        string Serialize<T>(T obj);

        T DeSerialize<T>(string content);
    }

    public interface IMessagePacker
    {
        string Pack(CapMessage obj);

        CapMessage UnPack(string packingMessage);
    }
}