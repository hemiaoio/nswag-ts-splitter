using System.Reflection;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.TypeScript;

namespace NSwagTsSplitter
{
    public static class TypeScriptGeneratorSettingsExtension
    {
        internal static ITemplate CreateTemplate(this TypeScriptGeneratorSettings instance, string typeName, object model)
        {
            MethodInfo methodInfo = instance.GetType().GetMethod(nameof(CreateTemplate),
               BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (methodInfo == null)
            {
                return null;
            }
            object[] paras = { typeName, model };
            return methodInfo.Invoke(instance, paras) as ITemplate;
        }
    }
}
