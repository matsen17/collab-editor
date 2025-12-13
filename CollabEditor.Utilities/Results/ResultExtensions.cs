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
    
    public static async Task<Result<T>> OnSuccessAsync<T>(
        this Result<T> result, 
        Func<T, Task> action)
    {
        if (result.IsSuccess)
        {
            await action(result.Value);
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
    
    public static async Task<Result> OnSuccessAsync(this Result result, Func<Task> action)
    {
        if (result.IsSuccess)
        {
            await action();
        }
        
        return result;
    }
    
    public static Result<T> OnFailure<T>(
        this Result<T> result, 
        Action<IEnumerable<IError>> action)
    {
        if (result.IsFailed)
        {
            action(result.Errors);
        }
        
        return result;
    }
    
    public static async Task<Result<T>> OnFailureAsync<T>(
        this Result<T> result, 
        Func<IEnumerable<IError>, Task> action)
    {
        if (result.IsFailed)
        {
            await action(result.Errors);
        }
        
        return result;
    }
    
    public static Result OnFailure(
        this Result result, 
        Action<IEnumerable<IError>> action)
    {
        if (result.IsFailed)
        {
            action(result.Errors);
        }
        return result;
    }
    
    public static async Task<Result> OnFailureAsync(
        this Result result, 
        Func<IEnumerable<IError>, Task> action)
    {
        if (result.IsFailed)
        {
            await action(result.Errors);
        }
        
        return result;
    }
}