using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Application.Common.Pagination;
using abc.bvl.AdminTool.Contracts.PilotEnablement;
using MediatR;

namespace abc.bvl.AdminTool.Application.PilotEnablement.Queries;

/// <summary>
/// Handler to retrieve pilot enablements (users with their screen assignments)
/// </summary>
public class GetPilotEnablementsHandler 
    : IRequestHandler<GetPilotEnablementsQuery, IEnumerable<PilotEnablementDto>>
{
    private readonly IScreenPilotRepository _pilotRepository;
    private readonly IScreenDefinitionRepository _screenRepository;

    public GetPilotEnablementsHandler(
        IScreenPilotRepository pilotRepository,
        IScreenDefinitionRepository screenRepository)
    {
        _pilotRepository = pilotRepository;
        _screenRepository = screenRepository;
    }

    public async Task<IEnumerable<PilotEnablementDto>> Handle(
        GetPilotEnablementsQuery request, 
        CancellationToken cancellationToken)
    {
        // Get all screens for reference (consider caching this)
        var allScreens = await _screenRepository.GetAllAsync(null, cancellationToken);
        var screenDict = allScreens.ToDictionary(s => s.ScreenGk, s => s);

        // ✅ Use generic paginated group query pattern
        var query = _pilotRepository
            .GetAllQueryable(request.StatusId)
            .GroupByPaginated(
                groupKeySelector: p => p.NbUserGk,
                resultSelector: g => new PilotEnablementDto
                {
                    NbUserGk = g.Key,
                    UserName = $"User {g.Key}", // TODO: Get actual user name from user table
                    ScreenAssignments = g.Select(pilot =>
                    {
                        var screen = screenDict.TryGetValue(pilot.ScreenGk, out var s) ? s : null;
                        return new ScreenDefnPilotUserDto
                        {
                            ScreenPilotGk = pilot.ScreenPilotGk,
                            ScreenGk = pilot.ScreenGk,
                            ScreenName = screen?.ScreenName ?? "Unknown",
                            StatusId = pilot.StatusId,
                            DualMode = pilot.DualMode,
                            AssignedDate = pilot.UpdatedDt,
                            AssignedBy = pilot.UpdatedBy,
                            ScreenStatusId = screen?.StatusId ?? 0
                        };
                    }).ToList()
                })
            .OrderGroupKeysBy(keys => keys.OrderBy(userGk => userGk));

        // Apply optional user filter
        if (request.NbUserGk.HasValue)
        {
            query = query.WhereGroupKey(keys => keys.Where(userGk => userGk == request.NbUserGk.Value));
        }

        // Apply pagination
        if (request.Pagination != null)
        {
            query = query.Paginate(request.Pagination.Page, request.Pagination.PageSize);
        }

        return await query.ExecuteAsync(cancellationToken);
    }
}
