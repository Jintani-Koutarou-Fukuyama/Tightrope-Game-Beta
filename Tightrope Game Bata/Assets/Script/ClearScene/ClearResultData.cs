public static class ClearResultData
{
    public static ClearResultManager.ClearRank LastClearRank { get; private set; }
    public static int LastMissCount { get; private set; }
    public static bool HasResult { get; private set; }

    public static void SetResult(ClearResultManager.ClearRank rank, int missCount)
    {
        LastClearRank = rank;
        LastMissCount = missCount;
        HasResult = true;
    }

    public static void Clear()
    {
        LastClearRank = ClearResultManager.ClearRank.S;
        LastMissCount = 0;
        HasResult = false;
    }
}
