namespace com.jobsite.chat.Shared.Infrastructure;

public static class JobsityChatCnnString
{
    public static string FromEnvironment() =>
        Environment.GetEnvironmentVariable(PersistenceKeys.JobsityChatCnnString)
        ?? throw new InvalidOperationException(
            $"Set the {PersistenceKeys.JobsityChatCnnString} environment variable " +
            "to the design-time connection string before running dotnet ef.");
}
