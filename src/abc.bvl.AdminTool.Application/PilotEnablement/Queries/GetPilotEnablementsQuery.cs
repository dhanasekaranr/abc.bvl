using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Contracts.PilotEnablement;
using MediatR;

namespace abc.bvl.AdminTool.Application.PilotEnablement.Queries;

/// <summary>
/// Query to get all pilots (users) with their screen assignments
/// This is the primary query for the UI
/// </summary>
public record GetPilotEnablementsQuery(
    long? NbUserGk = null,
    int? StatusId = null,
    PaginationRequest? Pagination = null
) : IRequest<IEnumerable<PilotEnablementDto>>;
