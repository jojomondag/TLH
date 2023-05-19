using System;
using System.Threading.Tasks;

public static class ExceptionHelper
{
    public static async Task TryCatchAsync(Func<Task> action, Action<Exception>? errorHandler = null)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            await MessageHelper.SaveErrorAsync($"Oops! Something went wrong: {ex.Message}");
            errorHandler?.Invoke(ex);
        }
    }

    public static T? TryCatch<T>(Func<T> action, Action<Exception>? errorHandler = null)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            MessageHelper.SaveError($"Oops! Something went wrong: {ex.Message}");
            errorHandler?.Invoke(ex);
            return default(T);
        }
    }

    public static async Task<T?> TryCatchAsync<T>(Func<Task<T>> action, Action<Exception>? errorHandler = null)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            await MessageHelper.SaveErrorAsync($"Oops! Something went wrong: {ex.Message}");
            errorHandler?.Invoke(ex);
            return default(T);
        }
    }

    public static void HandleException(Exception ex, string? errorMessage = null)
    {
        MessageHelper.SaveError(errorMessage ?? $"Oops! Something went wrong: {ex.Message}");
    }

    public static async Task<TResult> TryCatchAsync<TResult>(Func<Task<TResult>> tryBlock, Func<Exception, TResult> catchBlock)
    {
        try
        {
            return await tryBlock();
        }
        catch (Exception ex)
        {
            await MessageHelper.SaveErrorAsync($"Oops! Something went wrong: {ex.Message}");
            return catchBlock(ex);
        }
    }
}
