using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Contracts.ScreenDefinition;
using abc.bvl.AdminTool.Contracts.Common;
using MediatR;

namespace abc.bvl.AdminTool.Application.ScreenDefinition.Queries;

public record GetScreenDefinitionsQuery(byte? Status = null, PaginationRequest? Pagination = null) : IRequest<IEnumerable<ScreenDefnDto>>;

public record GetScreenDefinitionsCountQuery(byte? Status = null, string? SearchTerm = null) : IRequest<long>;

public class GetScreenDefinitionsHandler : IRequestHandler<GetScreenDefinitionsQuery, IEnumerable<ScreenDefnDto>>
{
    private readonly IScreenDefinitionRepository _repository;

    public GetScreenDefinitionsHandler(IScreenDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ScreenDefnDto>> Handle(GetScreenDefinitionsQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagination != null)
        {
            return await _repository.GetPagedAsync(
                request.Status, 
                request.Pagination.SearchTerm,
                request.Pagination, 
                cancellationToken);
        }

        return await _repository.GetAllAsync(request.Status, cancellationToken);
    }
}

public class GetScreenDefinitionsCountHandler : IRequestHandler<GetScreenDefinitionsCountQuery, long>
{
    private readonly IScreenDefinitionRepository _repository;

    public GetScreenDefinitionsCountHandler(IScreenDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<long> Handle(GetScreenDefinitionsCountQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetCountAsync(request.Status, request.SearchTerm, cancellationToken);
    }
}