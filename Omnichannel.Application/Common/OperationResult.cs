using System.Collections.Generic;

namespace Omnichannel.Application.Common
{
    public class OperationResult
    {
        public bool Succeeded { get; set; }
        public bool Success => Succeeded;
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public List<string> Errors { get; set; } = new();

        public static OperationResult Ok(string message = "Thao tác thành công.")
        {
            return new OperationResult
            {
                Succeeded = true,
                Message = message
            };
        }

        public static OperationResult Fail(string message, string? errorCode = null)
        {
            return new OperationResult
            {
                Succeeded = false,
                Message = message,
                ErrorCode = errorCode,
                Errors = new List<string> { message }
            };
        }
    }

    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; set; }

        public static OperationResult<T> Ok(T data, string message = "Thao tác thành công.")
        {
            return new OperationResult<T>
            {
                Succeeded = true,
                Data = data,
                Message = message
            };
        }

        public new static OperationResult<T> Fail(string message, string? errorCode = null)
        {
            return new OperationResult<T>
            {
                Succeeded = false,
                Message = message,
                ErrorCode = errorCode,
                Errors = new List<string> { message }
            };
        }
    }
}

