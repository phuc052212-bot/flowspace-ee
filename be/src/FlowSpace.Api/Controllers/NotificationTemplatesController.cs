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
    [Route("api/v1/notificationtemplates")]
    public class NotificationTemplatesController : BaseApiController
    {
        private readonly FlowSpaceDbContext _context;

        public NotificationTemplatesController(FlowSpaceDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<NotificationTemplate>>>> GetAll()
        {
            var templates = await _context.NotificationTemplates.ToListAsync();
            return OkResponse<IEnumerable<NotificationTemplate>>(templates, "Notification templates retrieved successfully.");
        }

        [Authorize(Policy = "ManagerOrAbove")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<NotificationTemplate>>> Create([FromBody] NotificationTemplate template)
        {
            if (string.IsNullOrWhiteSpace(template.Name) || string.IsNullOrWhiteSpace(template.Code))
            {
                return FailResponse<NotificationTemplate>("Template name and code are required.", StatusCodes.Status400BadRequest);
            }

            template.Id = Guid.NewGuid();
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.NotificationTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            return OkResponse(template, "Notification template created successfully.");
        }

        [Authorize(Policy = "ManagerOrAbove")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<NotificationTemplate>>> Update(Guid id, [FromBody] NotificationTemplate payload)
        {
            var template = await _context.NotificationTemplates.FindAsync(id);
            if (template == null)
            {
                return FailResponse<NotificationTemplate>("Notification template not found.", StatusCodes.Status404NotFound);
            }

            template.Code = payload.Code;
            template.Name = payload.Name;
            template.Subject = payload.Subject;
            template.Body = payload.Body;
            template.Channel = payload.Channel;
            template.IsActive = payload.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            _context.NotificationTemplates.Update(template);
            await _context.SaveChangesAsync();

            return OkResponse(template, "Notification template updated successfully.");
        }

        [Authorize(Policy = "ManagerOrAbove")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
        {
            var template = await _context.NotificationTemplates.FindAsync(id);
            if (template == null)
            {
                return FailResponse<string>("Notification template not found.", StatusCodes.Status404NotFound);
            }

            _context.NotificationTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return OkResponse("Notification template deleted successfully.");
        }
    }
}
