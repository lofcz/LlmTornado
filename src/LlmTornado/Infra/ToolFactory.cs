using System;
using System.Reflection;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Infra;

internal static class ToolFactory
{
    public static ToolFunction CreateFromMethod(Delegate del, IEndpointProvider provider)
    {
        ParameterInfo[] pars = del.Method.GetParameters();
        Tool function = new Tool
        {
            Params = []
        };

        foreach (ParameterInfo par in pars)
        {
            if (par.Name is null)
            {
                continue;
            }

            function.Params.Add(new ToolParam(par.Name, new ToolParamString("description", true)));
        }

        ToolFunction compiled = ChatPluginCompiler.Compile(function, new ToolMeta
        {
            Provider = provider
        });
        
        return compiled;
    }
}