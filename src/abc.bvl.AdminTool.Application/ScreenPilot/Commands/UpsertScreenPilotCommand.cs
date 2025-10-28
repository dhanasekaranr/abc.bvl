using abc.bvl.AdminTool.Contracts.ScreenPilot;
using MediatR;

namespace abc.bvl.AdminTool.Application.ScreenPilot.Commands;

/// <summary>
/// Command to upsert (create or update) a screen pilot assignment
/// Handler determines create vs update based on data existence
/// </summary>
public record UpsertScreenPilotCommand : IRequest<ScreenPilotDto>
{
    public long? Id { get; init; }
    public required long ScreenDefnId { get; init; }
    public required string UserId { get; init; }
    public byte Status { get; init; } = 1;
    public required string RequestedBy { get; init; }
}
