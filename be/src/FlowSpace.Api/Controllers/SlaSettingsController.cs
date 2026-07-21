using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowSpace.Application.Common.Dtos;
using FlowSpace.Persistence.Contexts;
using FlowSpace.Domain.Entities;

namespace FlowSpace.Api.Controllers
{
    [Authorize]
    [Route("api/v1/slasettings")]
    public class SlaSettingsController : BaseApiController
    {
        private readonly FlowSpaceDbContext _context;

        public SlaSettingsController(FlowSpaceDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<SlaSetting>>>> GetAll()
        {
            var settings = await _context.SlaSettings.ToListAsync();
            return OkResponse<IEnumerable<SlaSetting>>(settings, "SLA settings retrieved successfully.");
        }

        [Authorize(Policy = "ManagerOrAbove")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SlaSetting>>> Create([FromBody] SlaSetting sla)
        {
            if (string.IsNullOrWhiteSpace(sla.Name))
            {
                return FailResponse<SlaSetting>("SLA name is required.", StatusCodes.Status400BadRequest);
            }

            sla.Id = Guid.NewGuid();
            sla.CreatedAt = DateTime.UtcNow;
            sla.UpdatedAt = DateTime.UtcNow;

            await _context.SlaSettings.AddAsync(sla);
            await _context.SaveChangesAsync();

            return OkResponse(sla, "SLA setting created successfully.");
        }

        [Authorize(Policy = "ManagerOrAbove")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<SlaSetting>>> Update(Guid id, [FromBody] SlaSetting payload)
        {
            var sla = await _context.SlaSettings.FindAsync(id);
            if (sla == null)
            {
                return FailResponse<SlaSetting>("SLA setting not found.", StatusCodes.Status404NotFound);
            }

            sla.Name = payload.Name;
            sla.Priority = payload.Priority;
            sla.ResponseTimeHours = payload.ResponseTimeHours;
            sla.ResolutionTimeHours = payload.ResolutionTimeHours;
            sla.IsActive = payload.IsActive;
            sla.UpdatedAt = DateTime.UtcNow;

            _context.SlaSettings.Update(sla);
            await _context.SaveChangesAsync();

            return OkResponse(sla, "SLA setting updated successfully.");
        }

        [Authorize(Policy = "ManagerOrAbove")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
        {
            var sla = await _context.SlaSettings.FindAsync(id);
            if (sla == null)
            {
                return FailResponse<string>("SLA setting not found.", StatusCodes.Status404NotFound);
            }

            _context.SlaSettings.Remove(sla);
            await _context.SaveChangesAsync();

            return OkResponse("SLA setting deleted successfully.");
        }
    }
}
