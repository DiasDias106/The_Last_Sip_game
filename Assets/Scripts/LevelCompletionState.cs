public static class LevelCompletionState
{
    private const string Prefix = "__LEVEL_COMPLETED__";

    public static string GetKey(string sceneName)
    {
        return Prefix + sceneName;
    }
}
