using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using DotNetCore.CAP.Internal;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class ObjectMethodExecutorTest
    {

        [Fact]
        public void CanCreateInstance()
        {
            var testClass = new MethodExecutorClass();
            var methodInfo = testClass.GetType().GetMethod("Foo");

            var executor = ObjectMethodExecutor.Create(methodInfo, typeof(MethodExecutorClass).GetTypeInfo());

            Assert.NotNull(executor);
        }

        [Fact]
        public void CanExcuteMethodWithNoParameters()
        {
            var testClass = new MethodExecutorClass();
            var methodInfo = testClass.GetType().GetMethod("GetThree");

            var executor = ObjectMethodExecutor.Create(methodInfo, typeof(MethodExecutorClass).GetTypeInfo());

            Assert.NotNull(executor);

            var objResult = executor.Execute(testClass, null);

            Assert.Equal(3, objResult);
        }

        [Fact]
        public void CanExcuteMethodWithParameters()
        {
            var testClass = new MethodExecutorClass();
            var methodInfo = testClass.GetType().GetMethod("Add");

            var executor = ObjectMethodExecutor.Create(methodInfo, typeof(MethodExecutorClass).GetTypeInfo());

            Assert.NotNull(executor);

            var objResult = executor.Execute(testClass, 1, 2);

            Assert.Equal(3, objResult);
        }


        [Fact]
        public void CanGetExcuteMethodDefaultValue()
        {
            var testClass = new MethodExecutorClass();
            var methodInfo = testClass.GetType().GetMethod("WithDefaultValue");

            var executor = ObjectMethodExecutor.Create(methodInfo, typeof(MethodExecutorClass).GetTypeInfo());

            var objResult = executor.GetDefaultValueForParameter(0);
            Assert.Equal("aaa", objResult);

            var objResult2 = executor.GetDefaultValueForParameter(1);
            Assert.Equal("bbb", objResult2);
        }
    }

    public class MethodExecutorClass
    {
        public void Foo()
        {

        }

        public int GetThree()
        {
            return 3;
        }

        public int Add(int a, int b)
        {
            return a + b;
        }

        public void WithDefaultValue(string aaa = "aaa", string bbb = "bbb")
        {

        }
    }
}
