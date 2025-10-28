using abc.bvl.AdminTool.Application.Common.Interfaces;
using MediatR;

namespace abc.bvl.AdminTool.Application.ScreenDefinition.Commands;

/// <summary>
/// Handler for deleting a screen definition (soft delete)
/// </summary>
public class DeleteScreenDefinitionHandler : IRequestHandler<DeleteScreenDefinitionCommand, bool>
{
    private readonly IScreenDefinitionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteScreenDefinitionHandler(
        IScreenDefinitionRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<bool> Handle(DeleteScreenDefinitionCommand request, CancellationToken cancellationToken)
    {
        // Execute within transaction
        var result = await _unitOfWork.ExecuteAsync(async (ctx, ct) =>
        {
            // Get existing entity
            var entity = await _repository.GetEntityByIdAsync(request.Id, ct)
                ?? throw new KeyNotFoundException($"Screen definition with ID {request.Id} not found");

            // Soft delete (sets Status = 0)
            entity.MarkDeleted(request.DeletedBy);
            await _repository.UpdateAsync(entity, ct);
            await ctx.SaveChangesAsync(ct);
            
            return true;
        }, cancellationToken);

        return result;
    }
}
