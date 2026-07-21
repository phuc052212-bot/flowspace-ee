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
    [Route("api/v1/categories")]
    public class CategoriesController : BaseApiController
    {
        private readonly FlowSpaceDbContext _context;

        public CategoriesController(FlowSpaceDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<Category>>>> GetAll()
        {
            var categories = await _context.Categories
                .Where(c => !c.IsDeleted)
                .ToListAsync();
            return OkResponse<IEnumerable<Category>>(categories, "Categories retrieved successfully.");
        }

        [Authorize(Policy = "ManagerOrAbove")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Category>>> Create([FromBody] Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return FailResponse<Category>("Category name is required.", StatusCodes.Status400BadRequest);
            }

            category.Id = Guid.NewGuid();
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            category.IsDeleted = false;

            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            return OkResponse(category, "Category created successfully.");
        }

        [Authorize(Policy = "ManagerOrAbove")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<Category>>> Update(Guid id, [FromBody] Category payload)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null || category.IsDeleted)
            {
                return FailResponse<Category>("Category not found.", StatusCodes.Status404NotFound);
            }

            if (string.IsNullOrWhiteSpace(payload.Name))
            {
                return FailResponse<Category>("Category name is required.", StatusCodes.Status400BadRequest);
            }

            category.Name = payload.Name;
            category.Description = payload.Description;
            category.UpdatedAt = DateTime.UtcNow;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return OkResponse(category, "Category updated successfully.");
        }

        [Authorize(Policy = "ManagerOrAbove")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null || category.IsDeleted)
            {
                return FailResponse<string>("Category not found.", StatusCodes.Status404NotFound);
            }

            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return OkResponse("Category deleted successfully.");
        }
    }
}
