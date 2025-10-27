namespace abc.bvl.AdminTool.Application.Common.Interfaces;

public interface IRequestContext
{
    string UserId { get; }
    string DisplayName { get; }
    string Email { get; }
    string[] Roles { get; }
    string CorrelationId { get; }
    string DbRoute { get; }
    bool CanRead { get; }
    bool CanWrite { get; }
}