using abc.bvl.AdminTool.Application.Common.Interfaces;

namespace abc.bvl.AdminTool.Infrastructure.Data.Services;

public class RequestContextAccessor : IRequestContext
{
    public string UserId => "demo-user";
    public string DisplayName => "Demo User";
    public string Email => "demo@example.com";
    public string[] Roles => Array.Empty<string>();
    public string CorrelationId => Guid.NewGuid().ToString();
    public string DbRoute => "primary";
    public bool CanRead => true;
    public bool CanWrite => true;
}