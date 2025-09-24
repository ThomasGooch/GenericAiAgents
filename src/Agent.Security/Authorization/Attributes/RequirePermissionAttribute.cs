using Microsoft.AspNetCore.Authorization;

namespace Agent.Security.Authorization.Attributes;

/// <summary>
/// Authorization attribute that requires a specific permission
/// </summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"RequirePermission:{permission}";
    }
}

/// <summary>
/// Authorization attribute that requires admin role
/// </summary>
public class RequireAdminAttribute : AuthorizeAttribute
{
    public RequireAdminAttribute()
    {
        Policy = AuthorizationPolicies.AdminOnly;
    }
}

/// <summary>
/// Authorization attribute that requires service role
/// </summary>
public class RequireServiceAttribute : AuthorizeAttribute
{
    public RequireServiceAttribute()
    {
        Policy = AuthorizationPolicies.ServiceAccount;
    }
}

/// <summary>
/// Authorization attribute that requires admin or service role
/// </summary>
public class RequireAdminOrServiceAttribute : AuthorizeAttribute
{
    public RequireAdminOrServiceAttribute()
    {
        Policy = AuthorizationPolicies.AdminOrService;
    }
}

/// <summary>
/// Authorization attribute for workflow management operations
/// </summary>
public class RequireWorkflowManagerAttribute : AuthorizeAttribute
{
    public RequireWorkflowManagerAttribute()
    {
        Policy = AuthorizationPolicies.WorkflowManager;
    }
}

/// <summary>
/// Authorization attribute for system configuration operations
/// </summary>
public class RequireSystemConfiguratorAttribute : AuthorizeAttribute
{
    public RequireSystemConfiguratorAttribute()
    {
        Policy = AuthorizationPolicies.SystemConfigurator;
    }
}

/// <summary>
/// Authorization attribute for workflow operations
/// </summary>
public static class WorkflowAuthorization
{
    public class CreateAttribute : RequirePermissionAttribute
    {
        public CreateAttribute() : base(AgentPermissions.Workflow.Create) { }
    }

    public class ReadAttribute : RequirePermissionAttribute
    {
        public ReadAttribute() : base(AgentPermissions.Workflow.Read) { }
    }

    public class UpdateAttribute : RequirePermissionAttribute
    {
        public UpdateAttribute() : base(AgentPermissions.Workflow.Update) { }
    }

    public class DeleteAttribute : RequirePermissionAttribute
    {
        public DeleteAttribute() : base(AgentPermissions.Workflow.Delete) { }
    }

    public class ExecuteAttribute : RequirePermissionAttribute
    {
        public ExecuteAttribute() : base(AgentPermissions.Workflow.Execute) { }
    }
}

/// <summary>
/// Authorization attribute for system operations
/// </summary>
public static class SystemAuthorization
{
    public class ConfigureAttribute : RequirePermissionAttribute
    {
        public ConfigureAttribute() : base(AgentPermissions.System.Configure) { }
    }

    public class ViewMetricsAttribute : RequirePermissionAttribute
    {
        public ViewMetricsAttribute() : base(AgentPermissions.System.ViewMetrics) { }
    }

    public class ViewLogsAttribute : RequirePermissionAttribute
    {
        public ViewLogsAttribute() : base(AgentPermissions.System.ViewLogs) { }
    }

    public class ManageSecretsAttribute : RequirePermissionAttribute
    {
        public ManageSecretsAttribute() : base(AgentPermissions.System.ManageSecrets) { }
    }
}