using Engine.Scripts.Core.Chunks.States;

namespace Engine.Scripts.Core.Chunks.Managers
{
    public interface IChunkStateManager
    {
        void Init();
        void Reset();

        void MarkAsGenerated();

        bool CanUpdate();
        void Update();

        void RequestState(ChunkState state);

        bool IsStateCompleted(ChunkState state);
        bool IsSavePossible { get; }

        void SetMeshBuilt(); // temporary!
    }
}
