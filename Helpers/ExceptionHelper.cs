public static class ExceptionHelper
{
    public static async Task TryCatchAsync(Func<Task> action, Func<Exception, Task>? errorHandler = null)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            await MessageHelper.SaveErrorAsync($"Oops! Something went wrong: {ex.Message}");
            if (errorHandler != null)
            {
                await errorHandler(ex);
            }
        }
    }

    public static async Task<T?> TryCatchAsync<T>(Func<Task<T>> action, Func<Exception, Task>? errorHandler = null)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            await MessageHelper.SaveErrorAsync($"Oops! Something went wrong: {ex.Message}");
            if (errorHandler != null)
            {
                await errorHandler(ex);
            }
            return default(T);
        }
    }
    public static async Task HandleExceptionAsync(Exception ex, string? errorMessage = null)
    {
        await MessageHelper.SaveErrorAsync(errorMessage ?? $"Oops! Something went wrong: {ex.Message}");
    }
    public static async Task<TResult> TryCatchAsync<TResult>(Func<Task<TResult>> tryBlock, Func<Exception, Task<TResult>> catchBlock)
    {
        try
        {
            return await tryBlock();
        }
        catch (Exception ex)
        {
            await MessageHelper.SaveErrorAsync($"Oops! Something went wrong: {ex.Message}");
            return await catchBlock(ex);
        }
    }
}
