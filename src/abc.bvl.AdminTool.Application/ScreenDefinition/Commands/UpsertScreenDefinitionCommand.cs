using abc.bvl.AdminTool.Contracts.ScreenDefinition;
using MediatR;

namespace abc.bvl.AdminTool.Application.ScreenDefinition.Commands;

/// <summary>
/// Command to create or update a screen definition
/// Handler determines create vs update based on data existence in repository
/// </summary>
public record UpsertScreenDefinitionCommand : IRequest<ScreenDefnDto>
{
    public long? Id { get; init; }
    public required string ScreenName { get; init; }
    public string? Description { get; init; }
    public byte Status { get; init; } = 1;
    public required string RequestedBy { get; init; }
}
