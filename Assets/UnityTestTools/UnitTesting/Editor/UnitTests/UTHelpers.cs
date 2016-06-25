#if DEBUG
using Engine.Scripts.Common;
using NUnit.Framework;

namespace Assets.Engine.Scripts.UnitTesting
{
    [TestFixture]
    public class UTHelpers
    {
        [Test]
        public void Test_1D_2D_Coord ()
        {
            const int sidex = 16;
            const int sidey = 16;
            const int maxsize = sidex * sidey;

            for (int i=0; i<maxsize; i++) {
                int x, y;
                Helpers.GetIndex2DFrom1D (i, out x, out y, sidex);
			
                int index = Helpers.GetIndex1DFrom2D (x, y, sidex);
			
                Assert.AreEqual (i, index);
            }
        }

        [Test]
        public void Test_1D_3D_Coord ()
        {
            const int sidex = 16;
            const int sidey = 16;
            const int sidez = 16;
            const int maxsize = sidex * sidey * sidez;

            for (int i=0; i<maxsize; i++) {
                int x, y, z;
                Helpers.GetIndex3DFrom1D (i, out x, out y, out z, sidex, sidez);
			
                int index = Helpers.GetIndex1DFrom3D (x, y, z, sidex, sidez);
			
                Assert.AreEqual (i, index);
            }
        }
    }
}
#endif