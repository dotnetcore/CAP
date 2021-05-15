using System;
using DotNetCore.CAP.Internal;

namespace Sample.ConsoleApp
{
    public class SampleBootStrapperCallback:IBootstrapperCallback
    {
        public SampleBootStrapperCallback()
        {
        }

        public void BootStrappingFailed(Exception ex)
        {
            Console.WriteLine(ex);
        }

        public void BootStrappingStarted()
        {
            Console.WriteLine("#### BootStrappingStarted");
        }

        public void OnStart()
        {
            Console.WriteLine("#### OnStart");
        }

        public void OnStop()
        {
            Console.WriteLine("#### OnStop");
        }

        public void StorageInitFailed(Exception ex)
        {
            Console.WriteLine("#### StorageInitFailed");
        }

        public void StorageInitStarted()
        {
            Console.WriteLine("#### StorageInitStarted");
        }

        public void StorageInitSuccess()
        {
            Console.WriteLine("#### StorageInitSuccess");
        }
    }
}
