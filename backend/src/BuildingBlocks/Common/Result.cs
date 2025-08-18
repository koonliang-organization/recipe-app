namespace BuildingBlocks.Common;

public class Result
{
    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public string Error { get; }
    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
    
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result<T> Failure<T>(string error) => new(default!, false, error);
}

public class Result<T> : Result
{
    protected internal Result(T value, bool isSuccess, string error) 
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T Value { get; }
}