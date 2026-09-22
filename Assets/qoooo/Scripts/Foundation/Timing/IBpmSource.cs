namespace qoooo.Foundation.Timing
{
    /// <summary>clockを所有しないcomponent向けのread-only beatとtempo access。</summary>
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
