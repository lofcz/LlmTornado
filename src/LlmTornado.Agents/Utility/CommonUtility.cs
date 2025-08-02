namespace LlmTornado.Agents
{
    public static class EnumHelper
    {
        /// <summary>
        /// Helper to try to parse trying into enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static bool TryParseEnum<T>(this string input, out T? output)
        {
            try
            {
                Enum.TryParse(typeof(T), input, true, out object? result);
                output = (T?)result;
                return result != null;
            }
            catch (Exception)
            {
                output = default;
                return false;
            }
        }

        public static T ParseEnum<T>(this string input)
        {
            try
            {
                Enum.TryParse(typeof(T), input, true, out object? result);
                var output = (T?)result;
                return output;
            }
            catch (Exception)
            {
               throw new ArgumentException($"Failed to parse '{input}' into enum of type {typeof(T).Name}");

            }
        }
    }


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
