using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cap.Consistency.Job
{
    public class CapJob : IJob
    {

        public Task ExecuteAsync() {

            Console.WriteLine("当前时间：" + DateTime.Now.ToString());

            return Task.CompletedTask;
        }
    }
}
