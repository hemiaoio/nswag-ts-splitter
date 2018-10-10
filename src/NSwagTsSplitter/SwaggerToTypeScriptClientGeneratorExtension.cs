using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NJsonSchema.CodeGeneration.TypeScript;
using NSwag;
using NSwag.CodeGeneration;
using NSwag.CodeGeneration.TypeScript;
using NSwag.CodeGeneration.TypeScript.Models;

namespace NSwagTsSplitter
{
    internal static class SwaggerToTypeScriptClientGeneratorExtension
    {
        internal static List<TypeScriptOperationModel> GetOperations(this SwaggerToTypeScriptClientGenerator instance, TypeScriptTypeResolver _resolver, SwaggerDocument document)
        {
            // 这里本意是要使用 instance 的 GetOperations，但是发现Query中的Emun类型会被丢掉，故，直接拿源码重新处理_resolver里面的Type 
            document.GenerateOperationIds();

            return document.Paths
                .SelectMany(pair => pair.Value.Select(p => new { Path = pair.Key.TrimStart('/'), HttpMethod = p.Key, Operation = p.Value }))
                .Select(tuple =>
                {
                    var operationModel = new TypeScriptOperationModel(tuple.Operation, (SwaggerToTypeScriptClientGeneratorSettings)instance.BaseSettings, instance, _resolver);

                    operationModel.ControllerName = instance.BaseSettings.OperationNameGenerator.GetClientName(document, tuple.Path, tuple.HttpMethod, tuple.Operation);
                    operationModel.Path = tuple.Path;
                    operationModel.HttpMethod = tuple.HttpMethod;
                    operationModel.OperationName = instance.BaseSettings.OperationNameGenerator.GetOperationName(document, tuple.Path, tuple.HttpMethod, tuple.Operation);
                    return operationModel;
                })
                .ToList();
        }

        internal static string GenerateClientClass(this SwaggerToTypeScriptClientGenerator instance, TypeScriptTypeResolver _resolver, string controllerName, string controllerClassName, List<TypeScriptOperationModel> operations, ClientGeneratorOutputType type)
        {
            foreach (var operation in operations)
            {
                foreach (var response in operation.Responses.Where(r => r.HasType))
                {
                    response.DataConversionCode = DataConversionGenerator.RenderConvertToClassCode(new DataConversionParameters
                    {
                        Variable = "result" + response.StatusCode,
                        Value = "resultData" + response.StatusCode,
                        Schema = response.ActualResponseSchema,
                        IsPropertyNullable = response.IsNullable,
                        TypeNameHint = string.Empty,
                        Settings = instance.Settings.TypeScriptGeneratorSettings,
                        Resolver = _resolver,
                        NullValue = TypeScriptNullValue.Null
                    });
                }

                if (operation.HasDefaultResponse && operation.DefaultResponse.HasType)
                {
                    operation.DefaultResponse.DataConversionCode = DataConversionGenerator.RenderConvertToClassCode(new DataConversionParameters
                    {
                        Variable = "result",
                        Value = "resultData",
                        Schema = operation.DefaultResponse.ActualResponseSchema,
                        IsPropertyNullable = operation.DefaultResponse.IsNullable,
                        TypeNameHint = string.Empty,
                        Settings = instance.Settings.TypeScriptGeneratorSettings,
                        Resolver = _resolver,
                        NullValue = TypeScriptNullValue.Null
                    });
                }
            }
            List<string> typeNames = new List<string>();
            StringBuilder builder = new StringBuilder();
            foreach (var operation in operations)
            {
                foreach (var parameter in operation.Parameters)
                {
                    var parameterType = parameter.Type.IndexOf("[") > 0 ? parameter.Type.Replace("[]", "") : parameter.Type;
                    if (!typeNames.Contains(parameterType) && !Constant.TsBaseType.Contains(parameterType))
                    {
                        typeNames.Add(parameterType);
                    }
                }
                var resultType = operation.ResultType.IndexOf("[") > 0 ? operation.ResultType.Replace("[]", "") : operation.ResultType;
                if (!typeNames.Contains(resultType) && !Constant.TsBaseType.Contains(resultType))
                {
                    typeNames.Add(resultType);
                }
                var exceptionType = operation.ExceptionType.IndexOf("[") > 0 ? operation.ExceptionType.Replace("[]", "") : operation.ExceptionType;
                if (!typeNames.Contains(exceptionType) && !Constant.TsBaseType.Contains(exceptionType))
                {
                    typeNames.Add(exceptionType);
                }
            }
            typeNames.ForEach(c => builder.AppendLine($"import {{{c}}} from './{c}';"));
            // Append throwException
            builder.AppendLine($"import {{throwException}} from './Utilities';");
            builder.AppendLine();
            MethodInfo methodInfo = instance.GetType().GetMethod(nameof(GenerateClientClass),
              BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null)
            {
                return null;
            }
            object[] paras = { controllerName, controllerClassName, operations, type };
            var content = methodInfo.Invoke(instance, paras) as string;
            return builder.ToString() + content;
        }
    }
}