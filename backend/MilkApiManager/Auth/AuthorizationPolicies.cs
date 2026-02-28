namespace MilkApiManager.Auth;

public static class AuthorizationPolicies
{
    public const string ViewerOrAbove = "ViewerOrAbove";
    public const string OperatorOrAbove = "OperatorOrAbove";
    public const string AdminOnly = "AdminOnly";
}
