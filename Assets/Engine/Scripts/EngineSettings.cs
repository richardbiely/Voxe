using Engine.Scripts.Config;

namespace Engine.Scripts
{
    public static class EngineSettings
    {
        public static readonly CoreConfig CoreConfig = new CoreConfig();
        public static readonly ChunkConfig ChunkConfig = new ChunkConfig();
        public static readonly WorldConfig WorldConfig = new WorldConfig();
    }
}
