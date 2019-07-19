using System;
using System.Reflection;

namespace BlazorSignalR.Internal
{
    public static class ReflectionHelper
    {
        public static object CreateInstance(Assembly assembly, string typeName, params object[] args)
        {
            var type = assembly.GetType(typeName);
            return Activator.CreateInstance(type, args);
        }
    }
}