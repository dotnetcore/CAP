using System;
using SkyWalking.Utils;
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
            Assert.Equal(result, 2);
        }

        [Fact]
        public void Increment_Test()
        {
            var atomicInteger = new AtomicInteger(5);
            var result = atomicInteger.Increment();
            Assert.Equal(result, 6);
        }
        
        [Fact]
        public void Decrement_Test()
        {
            var atomicInteger = new AtomicInteger(5);
            var result = atomicInteger.Decrement();
            Assert.Equal(result, 4);
        }

        [Fact]
        public void Operator_Add_Test()
        {
            var atomicInteger = new AtomicInteger(5);
            var result = atomicInteger + 2;
            Assert.Equal<int>(result, 7);
        }
        
        [Fact]
        public void Operator_Sub_Test()
        {
            var atomicInteger = new AtomicInteger(5);
            var result = atomicInteger - 2;
            Assert.Equal<int>(result, 3);
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
    }
}