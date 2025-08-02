using BlazorWorker.Extensions.JSRuntime;
using NUnit.Framework;
using System.Collections.Generic;
using static BlazorWorker.Extensions.JSRuntime.DotNetObjectReferenceTracker;

namespace BlazorWorker.Extensions.JSRuntimeTests
{
    public class GenericNonPublicDelegateCacheTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GenericNonPublicDelegateCache_Returns_NonPublic_Delegates()
        {
            var genericDelegateCache = new GenericNonPublicDelegateCache(typeof(TypeWithPrivateThings));
            var instance = new TypeWithPrivateThings();
            var delegateTest = genericDelegateCache.GetDelegate<DoThingDelegate<long>, long, List<long>>(instance, "DoThing");

            var result = delegateTest.Invoke(new List<long>() { { 1L }, { 2L }, { 3L } });
            Assert.AreEqual(3+100, result);
        }

        public delegate long DoThingDelegate<T>(List<long> arg1);

        public class TypeWithPrivateThings {
#pragma warning disable IDE0051 // Remove unused private members - accessed by reflection
            private long DoThing<T>(List<T> arg1)
#pragma warning restore IDE0051 // Remove unused private members
            {
                return arg1.Count + 100;
            }
        }
    }
}