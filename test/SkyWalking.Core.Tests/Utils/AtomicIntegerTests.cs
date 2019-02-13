using System;
using SkyWalking.Common;
using Xunit;

namespace SkyWalking.Core.Tests.Utils
{
    public class AtomicIntegerTests
    {
        [Fact]
        public void Add_Test()
        {
            var atomicInteger = new AtomicInteger();
            var result = atomicInteger.Add(2);
            Assert.Equal(2, result);
        }

        [Fact]
        public void Increment_Test()
        {
            var atomicInteger = new AtomicInteger(5);
            var result = atomicInteger.Increment();
            Assert.Equal(6, result);
        }
        
        [Fact]
        public void Decrement_Test()
        {
            var atomicInteger = new AtomicInteger(5);
            var result = atomicInteger.Decrement();
            Assert.Equal(4, result);
        }

        [Fact]
        public void Operator_Add_Test()
        {
            var atomicInteger = new AtomicInteger(5);
            var result = atomicInteger + 2;
            Assert.Equal<int>(7, result);
        }
        
        [Fact]
        public void Operator_Sub_Test()
        {
            var atomicInteger = new AtomicInteger(5);
            var result = atomicInteger - 2;
            Assert.Equal<int>(3, result);
        }

        [Fact]
        public void Equals_Test()
        {
            AtomicInteger atomicInteger = 5;
            Assert.True(atomicInteger.Equals(5));
        }
        
        [Fact]
        public void Operator_Equals_Test()
        {
            AtomicInteger atomicInteger = 5;
            Assert.True(atomicInteger == 5);
            Assert.False(atomicInteger != 5);
        }

        [Fact]
        public void Set_Value()
        {
            AtomicInteger atomicInteger = 5;
            atomicInteger.Value = 10;
            Assert.Equal(10, atomicInteger.Value);
        }
    }
}