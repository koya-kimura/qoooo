namespace qoooo.Timing
{
    /// <summary>Read-only beat and tempo access for components that do not own the clock.</summary>
    public interface IBpmSource
    {
        double Bpm { get; }
        double Time { get; }
        double DeltaTime { get; }
        double Beat { get; }
        long BeatFloor { get; }

        int BeatModulo(int count);
        bool IsBeatModulo(int count, int target);
    }
}
