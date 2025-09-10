using System.Reflection;

namespace LlmTornado.StateMachines;

internal static class AsyncHelpers
{
    /// <summary>
    /// Checks if the type is a generic <see cref="Task"/> and returns the type of T if it is.
    /// </summary>
    public static bool IsGenericTask(Type type, out Type taskResultType)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                taskResultType = type;//type.GetGenericArguments()[0];
                return true;
            }

            type = type.BaseType!;
        }

        taskResultType = null;
        return false;
    }

    public static bool IsGenericValueTask(Type type, out Type taskResultType)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                taskResultType = type;//type.GetGenericArguments()[0];
                return true;
            }

            type = type.BaseType!;
        }

        taskResultType = null;
        return false;
    }

    public static async Task<object?> InvokeValueTaskFuncAsync(Delegate function, object[] args)
    {
        object? returnValue = function.DynamicInvoke(args);
        Type returnType = function.Method.ReturnType;
        object? result = null;
        if (IsGenericValueTask(returnType, out _))
        {
            // boxed ValueTask<T> -> call AsTask() via reflection -> await Task<T>
            MethodInfo asTask = returnType.GetMethod("AsTask")!;
            Task taskObj = (Task)asTask.Invoke(returnValue!, null)!;

            await taskObj.ConfigureAwait(false);
            PropertyInfo? resProp = taskObj.GetType().GetProperty("Result");
            result = resProp?.GetValue(taskObj);
        }
        else if (returnType == typeof(ValueTask))
        {
            // boxed ValueTask -> cast then await (or use AsTask())
            ValueTask vt = (ValueTask)returnValue!;
            await vt.ConfigureAwait(false); // or: await vt.AsTask().ConfigureAwait(false);
            result = null;
        }
        else
        {
            // synchronous
            result = returnValue;
        }
        return result;
    }

    /// <summary>
    /// Handles the actual Method Invoke async/sync and returns the result
    /// </summary>
    /// <param name="function"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task<object?> InvokeAsync(this Delegate function, object[] args)
    {
        object? returnValue = function.DynamicInvoke(args);
        Type returnType = function.Method.ReturnType;
        object? result = null;
        if (AsyncHelpers.IsGenericTask(returnType, out _))
        {
            Task? task = (Task?)returnValue;
            if (task is not null)
            {
                await task.ConfigureAwait(false);
                // for Task<T> get Result off the runtime type (safer)
                PropertyInfo? resProp = task.GetType().GetProperty("Result");
                result = resProp?.GetValue(task);
            }
        }
        else if (returnType == typeof(Task))
        {
            Task? task = (Task?)returnValue;
            if (task is not null)
            {
                await task.ConfigureAwait(false);
            }
        }
        else if (AsyncHelpers.IsGenericValueTask(returnType, out _))
        {
            // boxed ValueTask<T> -> call AsTask() via reflection -> await Task<T>
            MethodInfo asTask = returnType.GetMethod("AsTask")!;
            Task taskObj = (Task)asTask.Invoke(returnValue!, null)!;

            await taskObj.ConfigureAwait(false);
            PropertyInfo? resProp = taskObj.GetType().GetProperty("Result");
            result = resProp?.GetValue(taskObj);
        }
        else if (returnType == typeof(ValueTask))
        {
            // boxed ValueTask -> cast then await (or use AsTask())
            ValueTask vt = (ValueTask)returnValue!;
            await vt.ConfigureAwait(false); // or: await vt.AsTask().ConfigureAwait(false);
            result = null;
        }
        else
        {
            // synchronous
            result = returnValue;
        }
        return result;
    }
}