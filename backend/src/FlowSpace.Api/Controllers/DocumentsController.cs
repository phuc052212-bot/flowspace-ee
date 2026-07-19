using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlowSpace.Api.Controllers
{
    [Authorize]
    [Route("api/v1/documents")]
    public class DocumentsController : BaseApiController
    {
        private readonly IWebHostEnvironment _env;

        public DocumentsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<ApiResponse<object>>> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return FailResponse<object>("No file uploaded.", StatusCodes.Status400BadRequest);
            }

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/{uniqueFileName}";
            var result = new
            {
                id = Guid.NewGuid(),
                name = file.FileName,
                size = file.Length,
                type = file.ContentType,
                url = relativePath,
                uploadedAt = DateTime.UtcNow
            };

            return OkResponse<object>(result, "File uploaded successfully.");
        }
    }
}
