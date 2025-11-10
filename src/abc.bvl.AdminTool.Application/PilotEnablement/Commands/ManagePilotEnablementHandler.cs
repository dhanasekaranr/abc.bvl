using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Contracts.PilotEnablement;
using abc.bvl.AdminTool.Domain.Entities;
using MediatR;

namespace abc.bvl.AdminTool.Application.PilotEnablement.Commands;

/// <summary>
/// Handler for managing pilot enablement atomically
/// Handles all CRUD operations for user screen assignments
/// Works directly with PilotEnablementDto - no separate Command class needed
/// </summary>
public class ManagePilotEnablementHandler 
    : IRequestHandler<PilotEnablementDto, PilotEnablementDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScreenPilotRepository _pilotRepository;
    private readonly IScreenDefinitionRepository _screenRepository;

    public ManagePilotEnablementHandler(
        IUnitOfWork unitOfWork,
        IScreenPilotRepository pilotRepository,
        IScreenDefinitionRepository screenRepository)
    {
        _unitOfWork = unitOfWork;
        _pilotRepository = pilotRepository;
        _screenRepository = screenRepository;
    }

    public async Task<PilotEnablementDto> Handle(
        PilotEnablementDto request, 
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteAsync(async (ctx, ct) =>
        {
            // Process all screen assignments (add/update based on ScreenPilotGk)
            foreach (var assignment in request.ScreenAssignments)
            {
                if (assignment.ScreenPilotGk.HasValue)
                {
                    // Update existing assignment (including soft delete if statusId=0)
                    var existingPilot = await _pilotRepository.GetEntityByIdAsync(assignment.ScreenPilotGk.Value, ct);
                    if (existingPilot != null)
                    {
                        existingPilot.StatusId = assignment.StatusId;
                        existingPilot.DualMode = assignment.DualMode;
                        existingPilot.UpdatedBy = request.RequestedBy;
                        existingPilot.UpdatedDt = DateTime.UtcNow;
                        
                        await _pilotRepository.UpdateAsync(existingPilot, ct);
                    }
                }
                else
                {
                    // Add new assignment
                    var newPilot = new Domain.Entities.ScreenPilot
                    {
                        ScreenGk = assignment.ScreenGk,
                        NbUserGk = request.NbUserGk,
                        StatusId = assignment.StatusId,
                        DualMode = assignment.DualMode,
                        CreatedBy = request.RequestedBy,
                        UpdatedBy = request.RequestedBy,
                        CreatedDt = DateTime.UtcNow,
                        UpdatedDt = DateTime.UtcNow
                    };
                    
                    await _pilotRepository.CreateAsync(newPilot, ct);
                }
            }

            await ctx.SaveChangesAsync(ct);

            // Return updated pilot enablement data
            var updatedPilots = await _pilotRepository.GetByUserIdAsync(request.NbUserGk, ct);
            var allScreens = await _screenRepository.GetAllAsync(null, ct);
            var screenDict = allScreens.ToDictionary(s => s.ScreenGk, s => s);

            return new PilotEnablementDto
            {
                NbUserGk = request.NbUserGk,
                UserName = request.UserName,
                Email = request.Email,
                Department = request.Department,
                RequestedBy = request.RequestedBy,
                ScreenAssignments = updatedPilots.Select(pilot =>
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
            };

        }, cancellationToken);
    }
}
