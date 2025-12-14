using FluentResults;

namespace CollabEditor.Utilities.Results;

public static class ResultExtensions
{
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }
        
        return result;
    }
    
    public static Result OnSuccess(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }
        
        return result;
    }
    
    public static async Task<Result<T>> OnSuccessAsync<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess)
        {
            await action(result.Value);
        }
        
        return result;
    }
    
    public static async Task<Result> OnSuccessAsync(this Result result, Func<Task> action)
    {
        if (result.IsSuccess)
        {
            await action();
        }
        
        return result;
    }
    
    public static Result<T> OnFailure<T>(this Result<T> result, Action<IEnumerable<IError>> action)
    {
        if (result.IsFailed)
        {
            action(result.Errors);
        }
        
        return result;
    }
    
    public static async Task<Result<T>> OnFailureAsync<T>(this Result<T> result, Func<IEnumerable<IError>, Task> action)
    {
        if (result.IsFailed)
        {
            await action(result.Errors);
        }
        
        return result;
    }
    
    public static Result OnFailure(this Result result, Action<IEnumerable<IError>> action)
    {
        if (result.IsFailed)
        {
            action(result.Errors);
        }
        return result;
    }
    
    public static async Task<Result> OnFailureAsync(this Result result, Func<IEnumerable<IError>, Task> action)
    {
        if (result.IsFailed)
        {
            await action(result.Errors);
        }
        
        return result;
    }
    
    /// <summary>
    /// Ensures a condition is met, otherwise returns a failure.
    /// </summary>
    /// <example>
    /// return Result.Ok(user)
    ///     .Ensure(u => u.Age >= 18, "User must be 18 or older")
    ///     .Ensure(u => u.Email != null, "Email is required");
    /// </example>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
        if (result.IsFailed)
        {
            return result;
        }
        
        return predicate(result.Value) 
            ? result 
            : Result.Fail<T>(errorMessage);
    }
    
    public static async Task<Result<T>> EnsureAsync<T>(
        this Result<T> result,
        Func<T, Task<bool>> predicate,
        string errorMessage)
    {
        if (result.IsFailed)
        {
            return result;
        }
        
        var isValid = await predicate(result.Value);
        return isValid 
            ? result 
            : Result.Fail<T>(errorMessage);
    }
}