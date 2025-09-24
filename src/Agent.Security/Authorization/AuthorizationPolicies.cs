using Microsoft.AspNetCore.Authorization;

namespace Agent.Security.Authorization;

/// <summary>
/// Defines authorization policies for the Agent system
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy for admin-only operations
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Policy for authenticated users (any role)
    /// </summary>
    public const string AuthenticatedUser = "AuthenticatedUser";

    /// <summary>
    /// Policy for service accounts
    /// </summary>
    public const string ServiceAccount = "ServiceAccount";

    /// <summary>
    /// Policy for admin or service accounts
    /// </summary>
    public const string AdminOrService = "AdminOrService";

    /// <summary>
    /// Policy for users who can manage workflows
    /// </summary>
    public const string WorkflowManager = "WorkflowManager";

    /// <summary>
    /// Policy for users who can view system metrics
    /// </summary>
    public const string MetricsViewer = "MetricsViewer";

    /// <summary>
    /// Policy for users who can configure the system
    /// </summary>
    public const string SystemConfigurator = "SystemConfigurator";

    /// <summary>
    /// Configures authorization policies for the application
    /// </summary>
    /// <param name="options">Authorization options</param>
    public static void ConfigurePolicies(AuthorizationOptions options)
    {
        // Admin-only policy - requires Admin role
        options.AddPolicy(AdminOnly, policy =>
            policy.RequireRole(AgentRoles.Admin));

        // Authenticated user policy - requires any authenticated user
        options.AddPolicy(AuthenticatedUser, policy =>
            policy.RequireAuthenticatedUser());

        // Service account policy - requires Service role
        options.AddPolicy(ServiceAccount, policy =>
            policy.RequireRole(AgentRoles.Service));

        // Admin or Service policy - requires either Admin or Service role
        options.AddPolicy(AdminOrService, policy =>
            policy.RequireRole(AgentRoles.Admin, AgentRoles.Service));

        // Workflow manager policy - Admin or User with specific permission
        options.AddPolicy(WorkflowManager, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole(AgentRoles.Admin) ||
                (context.User.IsInRole(AgentRoles.User) && 
                 context.User.HasClaim("permission", "workflow:manage"))));

        // Metrics viewer policy - Any authenticated user can view metrics
        options.AddPolicy(MetricsViewer, policy =>
            policy.RequireAssertion(context =>
                context.User.Identity?.IsAuthenticated == true));

        // System configurator policy - Admin only with specific permission
        options.AddPolicy(SystemConfigurator, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole(AgentRoles.Admin) &&
                context.User.HasClaim("permission", "system:configure")));
    }
}

/// <summary>
/// Defines standard roles for the Agent system
/// </summary>
public static class AgentRoles
{
    /// <summary>
    /// Administrator role - full system access
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Regular user role - limited access
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Service account role - for system-to-system communication
    /// </summary>
    public const string Service = "Service";

    /// <summary>
    /// All available roles in the system
    /// </summary>
    public static readonly string[] AllRoles = { Admin, User, Service };
}

/// <summary>
/// Defines standard permissions for the Agent system
/// </summary>
public static class AgentPermissions
{
    /// <summary>
    /// Workflow management permissions
    /// </summary>
    public static class Workflow
    {
        public const string Create = "workflow:create";
        public const string Read = "workflow:read";
        public const string Update = "workflow:update";
        public const string Delete = "workflow:delete";
        public const string Execute = "workflow:execute";
        public const string Manage = "workflow:manage";
    }

    /// <summary>
    /// System configuration permissions
    /// </summary>
    public static class System
    {
        public const string Configure = "system:configure";
        public const string ViewMetrics = "system:metrics";
        public const string ViewLogs = "system:logs";
        public const string ManageSecrets = "system:secrets";
    }

    /// <summary>
    /// User management permissions
    /// </summary>
    public static class User
    {
        public const string Create = "user:create";
        public const string Read = "user:read";
        public const string Update = "user:update";
        public const string Delete = "user:delete";
        public const string ManageRoles = "user:roles";
    }
}