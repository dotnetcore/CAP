using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Abstractions
{
    public interface IContentSerializer
    {
        string Serialize<T>(T obj) where T : CapMessageDto, new();

        T DeSerialize<T>(string content) where T : CapMessageDto, new();
    }
}
