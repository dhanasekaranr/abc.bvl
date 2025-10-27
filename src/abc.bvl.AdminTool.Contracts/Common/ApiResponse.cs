namespace abc.bvl.AdminTool.Contracts.Common;

public record ApiResponse<T>(
    T Data,
    UserInfo User,
    AccessInfo Access,
    string CorrelationId,
    DateTimeOffset ServerTime
);

public record UserInfo(
    string UserId,
    string DisplayName,
    string Email
);

public record AccessInfo(
    bool CanRead,
    bool CanWrite,
    string[] Roles,
    string DbRoute
);