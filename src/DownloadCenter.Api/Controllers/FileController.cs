using DownloadCenter.Application.Files;
using DownloadCenter.Application.Files.Dtos;
using DownloadCenter.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace DownloadCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class FileController : ControllerBase
{
    private readonly FileService _fileService;

    public FileController(FileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("upload")]
    public async Task<ApiResult> Upload(IFormFile file)
    {
        return await _fileService.UploadAsync(file);
    }

    [HttpGet]
    public async Task<ApiResult> GetPage([FromQuery] FilePageInput input)
    {
        return await _fileService.GetPageAsync(input);
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(long id)
    {
        var result = await _fileService.GetDownloadAsync(id);
        if (result == null)
            return NotFound(ApiResult.Fail("文件不存在"));

        var (fullPath, originalName, extension) = result.Value;

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(originalName, out var contentType))
            contentType = "application/octet-stream";

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return File(stream, contentType, originalName);
    }

    [HttpGet("preview/{id}")]
    public async Task<IActionResult> Preview(long id)
    {
        var result = await _fileService.GetDownloadAsync(id);
        if (result == null)
            return NotFound(ApiResult.Fail("文件不存在"));

        var (fullPath, originalName, _) = result.Value;

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(originalName, out var contentType))
            contentType = "application/octet-stream";

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return File(stream, contentType);
    }

    [HttpDelete("{id}")]
    public async Task<ApiResult> Delete(long id)
    {
        return await _fileService.DeleteAsync(id);
    }
}
