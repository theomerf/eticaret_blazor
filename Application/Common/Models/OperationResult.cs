namespace Domain.Entities
{
    public class OperationResult
    {
        public bool IsSuccess { get; protected set; }
        public string Message { get; protected set; } = string.Empty;
        public ResultType Type { get; protected set; }
        public static OperationResult Success(string message = "")
            => new OperationResult
            {
                IsSuccess = true,
                Message = message,
                Type = ResultType.Success
            };
        public static OperationResult Failure(string message, ResultType type)
            => new OperationResult
            {
                IsSuccess = false,
                Message = message,
                Type = type
            };
    }

    public class OperationResult<T>
    {
        public bool IsSuccess { get; protected set; }
        public string Message { get; protected set; } = string.Empty;
        public ResultType Type { get; protected set; }
        public T? Data { get; protected set; }

        public static OperationResult<T> Success(string message = "")
            => new OperationResult<T>
            {
                IsSuccess = true,
                Message = message,
                Type = ResultType.Success
            };

        public static OperationResult<T> Success(T data, string message = "")
            => new OperationResult<T>
            {
                IsSuccess = true,
                Message = message,
                Type = ResultType.Success,
                Data = data
            };

        public static OperationResult<T> Failure(string message, ResultType type)
            => new OperationResult<T>
            {
                IsSuccess = false,
                Message = message,
                Type = type
            };
    }

    public enum ResultType
    {
        Success,
        ValidationError,
        BusinessError,
        ServiceUnavailable,
        NotFound,
        Unauthorized,
        Error
    }
}