using DownloadCenter.Application.Files.Dtos;
using DownloadCenter.Domain.Entities;
using DownloadCenter.Infrastructure.Persistence;
using DownloadCenter.Shared.Extensions;
using DownloadCenter.Shared.Models;
using DownloadCenter.Shared.Options;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DownloadCenter.Application.Files;

public class FileService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly FileStorageOptions _storageOptions;
    private readonly string _basePath;

    public FileService(AppDbContext db, IMapper mapper, IOptions<FileStorageOptions> storageOptions)
    {
        _db = db;
        _mapper = mapper;
        _storageOptions = storageOptions.Value;
        _basePath = Path.Combine(AppContext.BaseDirectory, _storageOptions.BasePath);
    }

    public async Task<ApiResult> UploadAsync(IFormFile file)
    {
        if (file.Length == 0)
            return ApiResult.Fail("文件为空");

        if (file.Length > _storageOptions.MaxSizeBytes)
            return ApiResult.Fail($"文件大小超过限制（最大 {_storageOptions.MaxSizeBytes / 1024 / 1024}MB）");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_storageOptions.AllowedExtensions.Contains(extension))
            return ApiResult.Fail($"不支持的文件类型：{extension}");

        var now = DateTime.Now;
        var dateFolder = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"), now.Day.ToString("D2"));
        var storedName = $"{Guid.NewGuid()}{extension}";
        var relativePath = Path.Combine(dateFolder, storedName);
        var fullDir = Path.Combine(_basePath, dateFolder);
        var fullPath = Path.Combine(_basePath, relativePath);

        Directory.CreateDirectory(fullDir);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        var record = new FileRecord
        {
            OriginalName = file.FileName,
            StoredName = storedName,
            Extension = extension,
            RelativePath = relativePath,
            Size = file.Length
        };

        _db.FileRecords.Add(record);
        await _db.SaveChangesAsync();

        return ApiResult.Ok(new { FileId = record.Id }, "上传成功");
    }

    public async Task<ApiResult> GetPageAsync(FilePageInput input)
    {
        var query = _db.FileRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(input.OriginalName))
            query = query.Where(f => f.OriginalName.Contains(input.OriginalName));

        query = query.OrderByDescending(f => f.CreatedAt);
        var totalCount = await query.CountAsync();
        var files = await query.Page(input).ToListAsync();
        var dtos = _mapper.Map<List<FileDto>>(files);
        return ApiResult.Ok(new PagedResult<FileDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = input.PageNumber,
            PageSize = input.PageSize
        });
    }

    public async Task<(string FullPath, string OriginalName, string Extension)?> GetDownloadAsync(long id)
    {
        var record = await _db.FileRecords.FindAsync(id);
        if (record == null)
            return null;

        var fullPath = Path.Combine(_basePath, record.RelativePath);
        if (!System.IO.File.Exists(fullPath))
            return null;

        return (fullPath, record.OriginalName, record.Extension);
    }

    public async Task<ApiResult> DeleteAsync(long id)
    {
        var record = await _db.FileRecords.FindAsync(id);
        if (record == null)
            return ApiResult.Fail("文件不存在");

        var fullPath = Path.Combine(_basePath, record.RelativePath);
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        _db.FileRecords.Remove(record);
        await _db.SaveChangesAsync();

        return ApiResult.Ok("删除成功");
    }
}
