using Xunit;
using ExternalModuleSubscriberClass = DotNetCore.CAP.MultiModuleSubscriberTests.SubscriberClass;

namespace DotNetCore.CAP.Test.SubscriberCollisionTests
{
    public class OldMethodTests
    {
            [Fact]
            public void NoCollision_SameClassAndMethod_DifferentAssemblies()
            {
                var methodInfo = typeof(SubscriberClass).GetMethod(nameof(SubscriberClass.TestSubscriber));
                var externalMethodInfo = typeof(ExternalModuleSubscriberClass).GetMethod(nameof(ExternalModuleSubscriberClass.TestSubscriber));

                var reflectedType = methodInfo.ReflectedType.Name;
                var key = $"{methodInfo.Module.Name}_{reflectedType}_{methodInfo.MetadataToken}";
                var externalReflectedType = methodInfo.ReflectedType.Name;
                var externalKey = $"{externalMethodInfo.Module.Name}_{externalReflectedType}_{externalMethodInfo.MetadataToken}";

                Assert.NotEqual(key, externalKey);
            }

            [Fact]
            public void NoCollision_Subclasses_SameAssembly()
            {
                var methodInfo1 = typeof(Subclass1OfSubscriberClass).GetMethod(nameof(SubscriberClass.TestSubscriber));
                var methodInfo2 = typeof(Subclass2OfSubscriberClass).GetMethod(nameof(SubscriberClass.TestSubscriber));

                var reflectedType = methodInfo1.ReflectedType.Name;
                var key = $"{methodInfo1.Module.Name}_{reflectedType}_{methodInfo1.MetadataToken}";
                var externalReflectedType = methodInfo2.ReflectedType.Name;
                var externalKey = $"{methodInfo2.Module.Name}_{externalReflectedType}_{methodInfo2.MetadataToken}";

                Assert.NotEqual(key, externalKey);
            }

            [Fact]
            public void Collision_SubclassOfGenericOpenType_SameAssembly_Handle()
            {
                var method1 = typeof(BaseClass<>)
                    .MakeGenericType(typeof(MessageType1))
                    .GetMethod(nameof(BaseClass<object>.Handle));
                var method2 = typeof(BaseClass<>)
                    .MakeGenericType(typeof(MessageType2))
                    .GetMethod(nameof(BaseClass<object>.Handle));

                Assert.Equal(method1.MetadataToken, method2.MetadataToken);
            }

            private class Subclass1OfSubscriberClass : SubscriberClass {}
            private class Subclass2OfSubscriberClass : SubscriberClass {}
            
            private class MessageType1 { }
            private class MessageType2 { }
            private abstract class BaseClass<T>
            {
                public void Handle()
                {
                }
            }
    }
}