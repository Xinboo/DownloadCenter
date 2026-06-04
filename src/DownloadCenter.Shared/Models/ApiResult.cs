namespace DownloadCenter.Shared.Models;

public class ApiResult
{
    public int Code { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ApiResult Ok(string? message = "success")
        => new() { Code = 200, Success = true, Message = message };

    public static ApiResult Fail(string? message = "fail")
        => new() { Code = 200, Success = false, Message = message };

    public static ApiResult Error(string? message = "服务器内部错误", int code = 500)
        => new() { Code = code, Success = false, Message = message };

    public static ApiResult<T> Ok<T>(T data, string? message = "success")
        => new() { Code = 200, Success = true, Message = message, Data = data };
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }
}
