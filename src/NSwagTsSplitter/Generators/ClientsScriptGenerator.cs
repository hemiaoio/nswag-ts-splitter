using NJsonSchema.CodeGeneration.TypeScript;

using NSwag;
using NSwag.CodeGeneration.TypeScript;
using NSwag.CodeGeneration.TypeScript.Models;

using NSwagTsSplitter.Helpers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NSwagTsSplitter.Contants;
using NJsonSchema.CodeGeneration;
using NSwag.CodeGeneration.OperationNameGenerators;
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
            settings.ExcludedParameterNames ??= Array.Empty<string>();
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
            var operations = GetAllOperationModels();
            foreach (var kv in GetGroupedOperations(operations))
            {
                var dtoImportCode = await GenerateDtoImportCode(kv.Value, kv.Key, outputDirectory);
                // generate client class
                var controllerClassName = _settings.GenerateControllerName(kv.Key);
                string path = Path.Combine(outputDirectory, controllerClassName + ".ts");
                IoHelper.Delete(path);

                var clientCode = GenerateClientClass(kv.Key, kv.Value);
                var commonImportCode = await CommonCodeGenerator.GetCommonImportFromUtilitiesAsync(outputDirectory, _utilitiesModuleName);
                clientCode = CommonCodeGenerator.AppendImport(clientCode, dtoImportCode);
                clientCode = CommonCodeGenerator.AppendImport(clientCode, commonImportCode);
                clientCode = CommonCodeGenerator.AppendDisabledLint(clientCode);

                await File.WriteAllTextAsync(path, clientCode, Encoding.UTF8);
            }
        }

        private async Task<string> GenerateDtoImportCode(TypeScriptOperationModel[] operations, string controllerName, string outputDirectory)
        {
            StringBuilder builder = new StringBuilder();

            var (typeNames, nswagTypes) = GetImportTypeList(operations);
            var controllerDtoPath = Path.Combine(outputDirectory, controllerName);
            if (Directory.Exists(controllerDtoPath))
            {
                Directory.Delete(controllerDtoPath, true);
            }

            Directory.CreateDirectory(controllerDtoPath);

            foreach (var typeName in typeNames)
            {
                var modelFile = $"./{(string.IsNullOrWhiteSpace(_dtoDirName) ? "" : _dtoDirName + "/")}{typeName}";
                var modelFileFullPath = Path.Combine(outputDirectory, $"{modelFile}.ts");
                if (!File.Exists(modelFileFullPath))
                {
                    continue;
                }
                var typeCode = await File.ReadAllTextAsync(modelFileFullPath, Encoding.UTF8);
                await MoveModelReferenceToController(typeCode, outputDirectory, controllerDtoPath);
                var originPath = Path.Combine(outputDirectory, $"{modelFile}.ts");
                var destinationPath = Path.Combine(controllerDtoPath, $"{modelFile}.ts");
                File.Copy(originPath, destinationPath, true);
                builder.AppendLine($"import {{ {typeName} }} from './{controllerName}/{typeName}';");
            }
            if (nswagTypes.Any())
            {
                var typeFile = $"./{_utilitiesModuleName}";
                var originPath = Path.Combine(outputDirectory, $"{typeFile}.ts");
                var destinationPath = Path.Combine(controllerDtoPath, $"{typeFile}.ts");
                File.Copy(originPath, destinationPath, true);
                builder.AppendLine($"import {{ {string.Join(", ", nswagTypes.Distinct())} }} from './{controllerName}/{_utilitiesModuleName}';");
            }

            await CommonCodeGenerator.GenerateIndexAsync(controllerDtoPath, false);

            builder.AppendLine();
            return builder.ToString();
        }

        private async Task MoveModelReferenceToController(string typeCode, string outputDirectory, string controllerDtoPath)
        {
            var typeCodeLines = typeCode.Split("\n");
            foreach (var typeCodeLine in typeCodeLines)
            {
                if (typeCodeLine.Trim().StartsWith("import"))
                {
                    var fromSubstring =
                        typeCodeLine.Substring(typeCodeLine.IndexOf("from", StringComparison.OrdinalIgnoreCase));
                    var file = fromSubstring.Replace("from", "").Replace(";", "").Replace("'", "").Replace("\"", "")
                        .Trim();
                    var originPath = Path.Combine(outputDirectory, $"{file}.ts");
                    var fileCode = await File.ReadAllTextAsync(originPath, Encoding.UTF8);
                    await MoveModelReferenceToController(fileCode, outputDirectory, controllerDtoPath);
                    var destinationPath = Path.Combine(controllerDtoPath, $"{file}.ts");
                    File.Copy(originPath, destinationPath, true);
                }
            }
        }

        public IEnumerable<KeyValuePair<string, TypeScriptOperationModel[]>> GetGroupedOperations(
            IEnumerable<TypeScriptOperationModel> operations)
        {
            var controllerOperationGroups = operations.GroupBy(o => o.ControllerName);
            foreach (var controllerOperations in controllerOperationGroups)
            {
                yield return new KeyValuePair<string, TypeScriptOperationModel[]>(controllerOperations.Key,
                    controllerOperations.ToArray());
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
            return clientCode;
        }


        public (List<string> typeNames, List<string> nswagTypeNames) GetImportTypeList(
            IEnumerable<TypeScriptOperationModel> operations)
        {
            List<string> typeNames = new List<string>();
            List<string> nswagTypes = new List<string>();
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

                // TODO: Handler generate type.
                var operationResultType = operation.ResultType.IndexOf("[", StringComparison.Ordinal) > 0
                    ? operation.ResultType.Replace("[]", "")
                    : operation.ResultType;
                operationResultType = operationResultType.Trim();
                var resultTypes = operationResultType.Split("|", StringSplitOptions.RemoveEmptyEntries);
                foreach (var type in resultTypes)
                {
                    var resultType = type.Trim();
                    if (!Constant.TsBaseType.Contains(resultType))
                    {
                        typeNames.Add(resultType);
                    }

                    if (Constant.UtilitiesModules.Contains(resultType))
                    {
                        nswagTypes.Add(resultType);
                    }
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
                    nswagTypes.Add(exceptionType);
                }
            }

            typeNames = typeNames.Where(c => !c.StartsWith("{ [key: "))
                .Distinct()
                .Where(c => !nswagTypes.Contains(c)).ToList();



            if (!string.IsNullOrWhiteSpace(_settings.ClientBaseClass))
            {
                nswagTypes.Add(_settings.ClientBaseClass);
            }

            if (_typeScriptClientGenerator.Settings.Template == TypeScriptTemplate.Axios)
            {
                nswagTypes.Add("isAxiosError");
            }

            if (_typeScriptClientGenerator.Settings.Template == TypeScriptTemplate.Angular)
            {
                nswagTypes.Add("blobToText");
                if (_typeScriptClientGenerator.Settings.UseGetBaseUrlMethod)
                {
                    nswagTypes.Add("API_BASE_URL");
                }
            }

            nswagTypes.Add("throwException");

            return (typeNames, nswagTypes);
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
            var codes = _generateClientTypesMethodInfo.Invoke(_typeScriptClientGenerator, paras) as IEnumerable<CodeArtifact>;
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
            _openApiDocument.GenerateOperationIds(true);

            var operationNameGenerator = _settings.OperationNameGenerator;
            return _openApiDocument.Paths
                .SelectMany(pair => pair.Value.Select(p => new
                { Path = pair.Key.TrimStart('/'), HttpMethod = p.Key, Operation = p.Value }))
                .Select(tuple =>
                {
                    var operationName =
                        operationNameGenerator.GetOperationName(_openApiDocument, tuple.Path,
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
                        : operationNameGenerator.GetClientName(_openApiDocument, tuple.Path,
                            tuple.HttpMethod, tuple.Operation);
                    operationModel.Path = tuple.Path;
                    operationModel.HttpMethod = tuple.HttpMethod;
                    if (operationNameGenerator is MultipleClientsFromPathSegmentsOperationNameGenerator)
                    {
                        if (operationName.Equals(operationModel.ControllerName + tuple.HttpMethod, StringComparison.OrdinalIgnoreCase))
                        {
                            operationName = tuple.HttpMethod;
                        }
                    }
                    //if (operationModel.PathParameters.Any())
                    //{
                    //    operationName += "ByPath";
                    //}
                    operationModel.OperationName = operationName;
                    return operationModel;
                });
        }
    }
}