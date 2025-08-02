namespace LlmTornado.StateMachines
{
    public static class AsyncHelpers
    {
        /// <summary>
        /// Checks if the type is a generic Task<T> and returns the type of T if it is.
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
    }
}
