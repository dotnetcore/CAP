using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Models;
using Newtonsoft.Json;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class Sample
    {
   
        public void DateTimeParam(DateTime dateTime)
        {
        }

        public void StringParam(string @string)
        {
        }

        public void GuidParam(Guid guid)
        {
        }

        public void UriParam(Uri uri)
        {
        }

        public void IntegerParam(int @int)
        {
        }

        public void ComplexTypeParam(ComplexType complexType)
        {
        }

        public void ThrowException()
        {
            throw new Exception();
        }

        public async Task<int> AsyncMethod()
        {
            await Task.FromResult(3);
            throw new Exception();
        }
    }

    public class ComplexType
    {
        public DateTime Time { get; set; }

        public string String { get; set; }

        public Guid Guid { get; set; }

        public Person Person { get; set; }
    }

    public class Person
    {
        public int Age { get; set; }

        public string Name { get; set; }
    }
}