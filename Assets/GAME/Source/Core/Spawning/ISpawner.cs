namespace JumpRing.Game.Core.Spawning
{
    public interface ISpawner
    {
        bool IsRunning { get; }

        void StartSpawning();

        void StopSpawning();
    }
}
