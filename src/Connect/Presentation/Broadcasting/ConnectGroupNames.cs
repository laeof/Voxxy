namespace Connect.Presentation.Broadcasting;

internal static class ConnectGroupNames
{
    public static string User(Guid userId) => $"connect:user:{userId:D}";
}
