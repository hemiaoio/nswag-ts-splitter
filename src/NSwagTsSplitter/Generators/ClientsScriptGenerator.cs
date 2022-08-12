using NJsonSchema.CodeGeneration.TypeScript;

using NSwag;
using NSwag.CodeGeneration.TypeScript;
using NSwag.CodeGeneration.TypeScript.Models;

using NSwagTsSplitter.Helpers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSwagTsSplitter.Contants;
using NJsonSchema.CodeGeneration;
using NSwagTsSplitter.Extensions;

namespace NSwagTsSplitter.Generators
{
    public class ClientsScriptGenerator
    {
        private readonly TypeScriptClientGeneratorSettings _settings;
        private readonly OpenApiDocument _openApiDocument;
        private string _dtoDirName = "";
        private string _utilitiesModuleName = "Utilities";
        private readonly TypeScriptTypeResolver _resolver;
        private readonly TypeScriptClientGenerator _typeScriptClientGenerator;

        private readonly MethodInfo _generateClientTypesMethodInfo = typeof(TypeScriptClientGenerator).GetMethod(
            "GenerateClientTypes",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        public ClientsScriptGenerator(TypeScriptClientGeneratorSettings settings, OpenApiDocument openApiDocument)
        {
            _settings = settings;
            _openApiDocument = openApiDocument;
            if (settings.ExcludedParameterNames == null)
            {
                settings.ExcludedParameterNames = Array.Empty<string>();
            }

            Constant.TsBaseType.AddRange(settings.ExcludedParameterNames);
            _resolver = new TypeScriptTypeResolver(settings.TypeScriptGeneratorSettings);
            _openApiDocument = openApiDocument;
            _resolver.RegisterSchemaDefinitions(_openApiDocument.Definitions);
            _typeScriptClientGenerator =
                new TypeScriptClientGenerator(_openApiDocument, settings, _resolver);
        }

        public void SetDtoPath(string dtoPathName)
        {
            _dtoDirName = dtoPathName;
        }

        public void SetUtilitiesModuleName(string utilitiesModuleName)
        {
            _utilitiesModuleName = utilitiesModuleName;
        }

        public async Task GenerateClientClassFilesAsync(string outputDirectory)
        {
            foreach (var clientClass in GenerateClientClasses())
            {
                string path = Path.Combine(outputDirectory, clientClass.Key + ".ts");
                IoHelper.Delete(path);
                var classCode = clientClass.Value;


                var commonImportCode = await CommonCodeGenerator.GetCommonImportFromUtilitiesAsync(outputDirectory, _utilitiesModuleName);
                classCode = commonImportCode + classCode;// CommonCodeGenerator.AppendImport(classCode, );
                classCode = CommonCodeGenerator.AppendDisabledLint(classCode);
                await File.WriteAllTextAsync(path, classCode, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Generate all classes
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, string>> GenerateClientClasses()
        {
            var operations = GetAllOperationModels();
            var controllerOperationGroups = operations.GroupBy(o => o.ControllerName);
            foreach (var controllerOperations in controllerOperationGroups)
            {
                var controllerClassName = _settings.GenerateControllerName(controllerOperations.Key);
                var clientCode = GenerateClientClass(controllerOperations.Key, controllerOperations.ToArray());
                yield return new KeyValuePair<string, string>(controllerClassName, clientCode);
            }
        }

        /// <summary>
        /// generate one service class
        /// </summary>
        /// <param name="className"></param>
        /// <param name="operationModels"></param>
        /// <returns></returns>
        public string GenerateClientClass(string className,
            TypeScriptOperationModel[] operationModels = null)
        {
            if (operationModels != null && operationModels.Any())
            {
                return GenerateClientClassWithOperationModels(className, operationModels);
            }

            var operations = GetAllOperationModels();
            operationModels = operations.GroupBy(o => o.ControllerName)
                .First(c => c.Key == className).ToArray();

            return GenerateClientClassWithOperationModels(className, operationModels);
        }

        /// <summary>
        /// generate one service class
        /// </summary>
        /// <param name="controllerName"></param>
        /// <param name="operations"></param>
        /// <returns></returns>
        public string GenerateClientClassWithOperationModels(string controllerName,
            TypeScriptOperationModel[] operations)
        {
            var controllerClassName = _settings.GenerateControllerName(controllerName);
            var clientCode =
                GenerateClientClassWithNameAndOperations(controllerName, controllerClassName, operations.ToList());
            return CommonCodeGenerator.AppendImport(clientCode, GetClientClassHeaderForImport(operations));
        }
        /// <summary>
        /// Get should be import dto and Utilities for operations
        /// </summary>
        /// <param name="operations"></param>
        /// <returns></returns>
        public string GetClientClassHeaderForImport(IEnumerable<TypeScriptOperationModel> operations)
        {
            List<string> typeNames = new List<string>();
            List<string> nswagTypes = new List<string>();
            StringBuilder builder = new StringBuilder();

            foreach (var operation in operations)
            {
                foreach (var parameter in operation.Parameters)
                {
                    var parameterType = parameter.Type.IndexOf("[", StringComparison.Ordinal) > 0
                        ? parameter.Type.Replace("[]", "")
                        : parameter.Type;
                    if (!Constant.TsBaseType.Contains(parameterType))
                    {
                        typeNames.Add(parameterType);
                    }

                    if (Constant.UtilitiesModules.Contains(parameterType))
                    {
                        nswagTypes.Add(parameterType);
                    }
                }

                var resultType = operation.ResultType.IndexOf("[", StringComparison.Ordinal) > 0
                    ? operation.ResultType.Replace("[]", "")
                    : operation.ResultType;
                if (!Constant.TsBaseType.Contains(resultType))
                {
                    typeNames.Add(resultType);
                }

                if (Constant.UtilitiesModules.Contains(resultType))
                {
                    nswagTypes.Add(resultType);
                }

                var exceptionType = operation.ExceptionType.IndexOf("[", StringComparison.Ordinal) > 0
                    ? operation.ExceptionType.Replace("[]", "")
                    : operation.ExceptionType;
                var exceptionTypes = exceptionType.Split("|").Select(c => c.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Where(c => !Constant.TsBaseType.Contains(c))
                    .Distinct();
                typeNames.AddRange(exceptionTypes);
                if (Constant.UtilitiesModules.Contains(exceptionType))
                {
                    nswagTypes.Add(resultType);
                }
            }

            typeNames.Where(c => !c.StartsWith("{ [key: "))
                .Distinct()
                .Where(c => !nswagTypes.Contains(c))
                .ForEach(c =>
                    builder.AppendLine(
                        $"import {{ {c} }} from './{(string.IsNullOrWhiteSpace(_dtoDirName) ? "" : _dtoDirName + "/")}{c}';"));

            if (!string.IsNullOrWhiteSpace(_settings.ClientBaseClass))
            {
                nswagTypes.Add(_settings.ClientBaseClass);
            }
            if (_typeScriptClientGenerator.Settings.Template == TypeScriptTemplate.Axios)
            {
                nswagTypes.Add("isAxiosError");
            }
            nswagTypes.Add("throwException");
            if (nswagTypes.Any())
            {
                builder.AppendLine(
                    $"import {{ {string.Join(",", nswagTypes.Distinct())} }} from './{_utilitiesModuleName}';");
            }

            builder.AppendLine();

            return builder.ToString();
        }
        /// <summary>
        /// with custom class name for target controller name
        /// </summary>
        /// <param name="controllerName"></param>
        /// <param name="controllerClassName"></param>
        /// <param name="operations"></param>
        /// <returns></returns>
        public virtual string GenerateClientClassWithNameAndOperations(string controllerName,
            string controllerClassName, IEnumerable<TypeScriptOperationModel> operations)
        {
            object[] paras = { controllerName, controllerClassName, operations };
            var codes =
                _generateClientTypesMethodInfo.Invoke(_typeScriptClientGenerator, paras) as IEnumerable<CodeArtifact>;

            return string.Join("\n", codes.Select(c => c.Code));
        }

        /// <summary>
        /// get the api document all operations
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<TypeScriptOperationModel> GetAllOperationModels()
        {
            // The intention here is to use the GetOperations of instance, but found that the Emun
            // type in Query will be thrown away, so directly take the source code to re-process the
            // type inside _resolver
            _openApiDocument.GenerateOperationIds();
            return _openApiDocument.Paths
                .SelectMany(pair => pair.Value.Select(p => new
                { Path = pair.Key.TrimStart('/'), HttpMethod = p.Key, Operation = p.Value }))
                .Select(tuple =>
                {
                    var operationName =
                        _settings.OperationNameGenerator.GetOperationName(_openApiDocument, tuple.Path,
                            tuple.HttpMethod, tuple.Operation);
                    if (operationName.EndsWith("Async"))
                    {
                        operationName = operationName.Substring(0, operationName.Length - "Async".Length);
                    }

                    var operationModel = new TypeScriptOperationModel(tuple.Operation, _settings,
                        _typeScriptClientGenerator,
                        _resolver); // CreateOperationModel(tuple.Operation, _clientGeneratorSettings);
                    operationModel.ControllerName = tuple.Operation.Tags.Any()
                        ? tuple.Operation.Tags.First()
                        : _settings.OperationNameGenerator.GetClientName(_openApiDocument, tuple.Path,
                            tuple.HttpMethod, tuple.Operation);
                    operationModel.Path = tuple.Path;
                    operationModel.HttpMethod = tuple.HttpMethod;
                    if (operationModel.PathParameters.Any())
                    {
                        operationName += "ByPath";
                    }

                    operationModel.OperationName = operationName;
                    return operationModel;
                });
        }
    }
}