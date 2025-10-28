using abc.bvl.AdminTool.Contracts.ScreenPilot;
using MediatR;

namespace abc.bvl.AdminTool.Application.ScreenPilot.Queries;

/// <summary>
/// Query to get screen pilots by user ID
/// </summary>
public record GetUserScreenPilotsQuery(string UserId) : IRequest<IEnumerable<ScreenPilotDto>>;
