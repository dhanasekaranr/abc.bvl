using MediatR;

namespace abc.bvl.AdminTool.Application.ScreenDefinition.Commands;

/// <summary>
/// Command to delete a screen definition (soft delete)
/// </summary>
public record DeleteScreenDefinitionCommand : IRequest<bool>
{
    public required long Id { get; init; }
    public required string DeletedBy { get; init; }
}
