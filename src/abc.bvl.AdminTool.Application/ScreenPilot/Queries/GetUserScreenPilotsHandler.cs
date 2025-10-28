using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Contracts.ScreenPilot;
using MediatR;

namespace abc.bvl.AdminTool.Application.ScreenPilot.Queries;

/// <summary>
/// Handler for getting user screen pilots
/// </summary>
public class GetUserScreenPilotsHandler : IRequestHandler<GetUserScreenPilotsQuery, IEnumerable<ScreenPilotDto>>
{
    private readonly IScreenPilotRepository _repository;

    public GetUserScreenPilotsHandler(IScreenPilotRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<ScreenPilotDto>> Handle(GetUserScreenPilotsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}
