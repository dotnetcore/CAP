using System;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Job
{
    public class CapJob : IJob
    {
        public Task ExecuteAsync()
        {
            Console.WriteLine("当前时间：" + DateTime.Now.ToString());

            return Task.CompletedTask;
        }
    }
}