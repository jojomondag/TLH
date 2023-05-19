using System;
using System.Threading.Tasks;

namespace TLH
{
    // A helper class to catch exceptions and display them to the user, utilizing the MessageHelper class if needed.
    internal static class ExceptionHelper
    {
        public static async Task TryCatchAsync(Func<Task> action, Action<Exception>? errorHandler = null)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oops! Something went wrong: {ex.Message}");
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
                Console.WriteLine($"Oops! Something went wrong: {ex.Message}");
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
                Console.WriteLine($"Oops! Something went wrong: {ex.Message}");
                errorHandler?.Invoke(ex);
                return default(T);
            }
        }
        public static void HandleException(Exception ex, string? errorMessage = null)
        {
            Console.WriteLine(errorMessage ?? $"Oops! Something went wrong: {ex.Message}");
        }
        public static async Task<TResult> TryCatchAsync<TResult>(Func<Task<TResult>> tryBlock, Func<Exception, TResult> catchBlock)
        {
            try
            {
                return await tryBlock();
            }
            catch (Exception ex)
            {
                return catchBlock(ex);
            }
        }
    }
}