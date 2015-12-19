#if DEBUG
using System.Collections.Generic;
using Assets.Engine.Scripts.Common.Collections;
using NUnit.Framework;

namespace Assets.Engine.Scripts.UnitTesting
{
    [TestFixture]
    public class UTObjectPool
    {
        class TestClass
        {
        }

        [Test]
        public void Pop ()
        {
            const int n = 16;

            TestClass c = null;
            ObjectPool<TestClass> pool = new ObjectPool<TestClass> (() => new TestClass (), n);

            for (int i=0; i<n; i++) {
                c = pool.Pop ();
            }

            pool.Push (c);
        }

        [Test]
        public void Push ()
        {
            const int n = 16;

            List<TestClass> list = new List<TestClass> ();
            ObjectPool<TestClass> pool = new ObjectPool<TestClass> (() => new TestClass (), n);

            for (int i=0; i<n; i++) {
                list.Add (pool.Pop ());
            }

            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                pool.Push(c);
            }
        }
    }
}
#endif