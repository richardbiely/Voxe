namespace Engine.Scripts.Rendering.Batchers
{
    public interface IGeometryBatcher
    {
        void Clear();
        void Commit();
        void Enable(bool enable);
        bool IsEnabled();
    }
}
