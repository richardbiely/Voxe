using System.IO;

namespace Assets.Engine.Scripts.Common.IO
{
    public interface IBinarizable
    {
        void Binarize(BinaryWriter bw);
        void Debinarize(BinaryReader br);
    }
}
