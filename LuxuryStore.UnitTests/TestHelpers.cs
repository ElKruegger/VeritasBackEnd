namespace LuxuryStore.UnitTests;

internal static class TestHelpers
{
    /// <summary>
    /// xUnit v3 cancels this token when a test times out or the run is
    /// aborted — passing it into async calls keeps a hung test from blocking
    /// the whole suite (and satisfies analyzer rule xUnit1051).
    /// </summary>
    public static CancellationToken Ct => TestContext.Current.CancellationToken;
}
