using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.CodeGeneration.TypeScript.Models;
using NSwag;
using NSwag.CodeGeneration.TypeScript;
using NSwag.CodeGeneration.TypeScript.Models;

namespace NSwagTsSplitter
{
    public class SelfTypeScriptGenerator
    {
        private readonly TypeScriptClientGeneratorSettings _clientGeneratorSettings;
        private readonly TypeScriptTypeResolver _resolver;
        private readonly OpenApiDocument _openApiDocument;
        private readonly TypeScriptClientGenerator _typeScriptClientGenerator;
        private readonly TypeScriptGenerator _typeScriptGenerator;
        private readonly TypeScriptExtensionCode _extensionCode;
        private string _utilitiesModuleName = "Utilities";

        private readonly MethodInfo _generateClientTypesMethodInfo = typeof(TypeScriptClientGenerator).GetMethod(
            "GenerateClientTypes",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly MethodInfo _generateTypeMethodInfo =
            typeof(TypeScriptGenerator).GetMethod("GenerateType",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);


        public SelfTypeScriptGenerator(TypeScriptClientGeneratorSettings clientGeneratorSettings,
            OpenApiDocument openApiDocument)
        {
            _clientGeneratorSettings = clientGeneratorSettings;
            _resolver = new TypeScriptTypeResolver(clientGeneratorSettings.TypeScriptGeneratorSettings);
            _openApiDocument = openApiDocument;
            _resolver.RegisterSchemaDefinitions(_openApiDocument.Definitions);
            _typeScriptClientGenerator =
                new TypeScriptClientGenerator(_openApiDocument, _clientGeneratorSettings, _resolver);
            _typeScriptGenerator = new TypeScriptGenerator(_openApiDocument,
                clientGeneratorSettings.TypeScriptGeneratorSettings, _resolver);
            _extensionCode = new TypeScriptExtensionCode(
                clientGeneratorSettings.TypeScriptGeneratorSettings.ExtensionCode,
                (clientGeneratorSettings.TypeScriptGeneratorSettings.ExtendedClasses ?? new string[] { })
                .Concat(new[] {clientGeneratorSettings.ConfigurationClass}).ToArray(),
                new[] {clientGeneratorSettings.ClientBaseClass});
        }

        #region ClientClass

        /// <summary>
        /// generate one service class
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public string GenerateClientClass(string className)
        {
            var operations = GetAllOperationModels();
            var controllerOperations = operations.GroupBy(o => o.ControllerName)
                .First(c => c.Key == className);
            return GenerateClientClassWithOperationModels(className, controllerOperations.ToList());
        }


        /// <summary>
        /// generate one service class
        /// </summary>
        /// <param name="controllerName"></param>
        /// <param name="operations"></param>
        /// <returns></returns>
        public string GenerateClientClassWithOperationModels(string controllerName,
            IEnumerable<TypeScriptOperationModel> operations)
        {
            var controllerClassName = _clientGeneratorSettings.GenerateControllerName(controllerName);
            var clientCode =
                GenerateClientClassWithNameAndOperations(controllerName, controllerClassName, operations.ToList());
            return GetClientClassHeaderForImport(operations) + clientCode;
        }

        /// <summary>
        /// Generate all classes
        /// </summary>
        /// <param name="outputDirectory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IDictionary<string, string> GenerateClientClasses()
        {
            var classCodes = new Dictionary<string, string>();
            var operations = GetAllOperationModels();
            var controllerOperationGroups = operations.GroupBy(o => o.ControllerName);
            foreach (var controllerOperations in controllerOperationGroups)
            {
                var controllerClassName = _clientGeneratorSettings.GenerateControllerName(controllerOperations.Key);
                var clientCode = GenerateClientClass(controllerOperations.Key);
                classCodes.Add(controllerClassName, clientCode);
            }

            return classCodes;
        }

        /// <summary>
        /// Generate Client Class With ApiOperations
        /// </summary>
        /// <param name="controllerName"></param>
        /// <param name="operationDescriptions"></param>
        /// <returns></returns>
        public string GenerateClientClassWithApiOperations(string controllerName,
            IEnumerable<OpenApiOperationDescription> operationDescriptions)
        {
            var modelOperations = operationDescriptions.Select(GetOperationModelByApiOperation);
            return GenerateClientClassWithOperationModels(controllerName, modelOperations);
        }


        /// <summary>
        /// with custom class name for target controller name
        /// </summary>
        /// <param name="controllerName"></param>
        /// <param name="controllerClassName"></param>
        /// <param name="operations"></param>
        /// <returns></returns>
        public string GenerateClientClassWithNameAndOperations(string controllerName,
            string controllerClassName, IEnumerable<TypeScriptOperationModel> operations)
        {
            object[] paras = {controllerName, controllerClassName, operations};
            var codes =
                _generateClientTypesMethodInfo.Invoke(_typeScriptClientGenerator, paras) as IEnumerable<CodeArtifact>;

            return string.Join("\n", codes.Select(c => c.Code));
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
            if (!string.IsNullOrWhiteSpace(_clientGeneratorSettings.ClientBaseClass))
            {
                typeNames.Add(_clientGeneratorSettings.ClientBaseClass);
            }

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
                if (!Constant.TsBaseType.Contains(exceptionType))
                {
                    typeNames.Add(exceptionType);
                }

                if (Constant.UtilitiesModules.Contains(exceptionType))
                {
                    nswagTypes.Add(resultType);
                }
            }

            typeNames.Distinct().Where(c => !nswagTypes.Contains(c)).ToList()
                .ForEach(c => builder.AppendLine($"import {{ {c} }} from './{c}';"));

            nswagTypes.Add("throwException");
            if (nswagTypes.Any())
            {
                builder.AppendLine(
                    $"import {{ {string.Join(",", nswagTypes.Distinct())} }} from './{_utilitiesModuleName}';");
            }

            builder.AppendLine();
            return builder.ToString();
        }

        #endregion

        #region DtoClass

        public IDictionary<string, string> GenerateDtoClasses()
        {
            var dtos = new Dictionary<string, string>();
            foreach (var definition in _openApiDocument.Definitions)
            {
                var code = GenerateDtoClass(definition.Value, definition.Key, out string className);
                dtos.Add(className, code);
            }

            return dtos;
        }

        /// <summary>
        /// Generate Dto Class
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="typeNameHint"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        public string GenerateDtoClass(JsonSchema schema, string typeNameHint, out string className)
        {
            //CodeArtifact modelClassArtifact =
            //    _generateTypeMethodInfo.Invoke(_typeScriptGenerator, new object[] {schema, typeNameHint}) as
            //        CodeArtifact;
            //if (modelClassArtifact.Type == CodeArtifactType.Class || modelClassArtifact.Type == CodeArtifactType.Interface)
            //{
            //    var model = new ClassTemplateModel(modelClassArtifact.TypeName, typeNameHint, _clientGeneratorSettings.TypeScriptGeneratorSettings, _resolver, schema, _openApiDocument.Definitions);
            //    var template = _clientGeneratorSettings.TypeScriptGeneratorSettings.CreateTemplate(modelClassArtifact.TypeName, model);

            //    var type = _clientGeneratorSettings.TypeScriptGeneratorSettings.TypeStyle == TypeScriptTypeStyle.Interface
            //        ? CodeArtifactType.Interface
            //        : CodeArtifactType.Class;

            //    modelClassArtifact = new CodeArtifact(modelClassArtifact.TypeName, model.BaseClass, type, CodeArtifactLanguage.TypeScript, CodeArtifactCategory.Contract, template);
            //}

            //return string.Join("\n", types.Select(c => c.Code));
            string appendCode = string.Empty;
            var typeName = _resolver.GetOrGenerateTypeName(schema, typeNameHint);

            if (schema.IsEnumeration)
            {
                var model = new EnumTemplateModel(string.IsNullOrWhiteSpace(typeName) ? typeNameHint : typeName, schema,
                    _resolver.Settings);
                var template = _resolver.Settings.TemplateFactory.CreateTemplate("TypeScript", "Enum", model);
                var codeArtifact = new CodeArtifact(string.IsNullOrWhiteSpace(typeName) ? typeNameHint : typeName,
                    CodeArtifactType.Enum, CodeArtifactLanguage.TypeScript, CodeArtifactCategory.Undefined, template);
                className = codeArtifact.TypeName;

                return codeArtifact.Code;
            }
            else
            {
                var model = new ClassTemplateModel(typeName, typeNameHint, _resolver.Settings, _resolver, schema,
                    schema);
                List<string> typeNames = new List<string>();
                List<string> nswagTypes = new List<string>();
                StringBuilder builder = new StringBuilder();
                foreach (var actualProperty in schema.ActualProperties)
                {
                    if (actualProperty.Value.IsEnumeration && actualProperty.Value.Reference == null)
                    {
                        appendCode = GenerateDtoClass(actualProperty.Value, actualProperty.Key, out _);
                    }

                    var property = new PropertyModel(model, actualProperty.Value, actualProperty.Key, _resolver,
                        _resolver.Settings);

                    var propertyType = property.Type.IndexOf("[", StringComparison.Ordinal) > 0
                        ? property.Type.Replace("[]", "")
                        : property.Type;
                    if (!Constant.TsBaseType.Contains(propertyType) && !property.IsDictionary &&
                        !actualProperty.Value.IsEnumeration)
                    {
                        typeNames.Add(propertyType);
                    }

                    if (Constant.UtilitiesModules.Contains(propertyType))
                    {
                        nswagTypes.Add(propertyType);
                    }

                    var propertyDictionaryItemType =
                        property.DictionaryItemType.IndexOf("[", StringComparison.Ordinal) > 0
                            ? property.DictionaryItemType.Replace("[]", "")
                            : property.DictionaryItemType;
                    if (!Constant.TsBaseType.Contains(propertyDictionaryItemType))
                    {
                        typeNames.Add(propertyDictionaryItemType);
                    }

                    if (Constant.UtilitiesModules.Contains(propertyDictionaryItemType))
                    {
                        nswagTypes.Add(propertyDictionaryItemType);
                    }

                    var propertyArrayItemType = property.ArrayItemType.IndexOf("[", StringComparison.Ordinal) > 0
                        ? property.ArrayItemType.Replace("[]", "")
                        : property.ArrayItemType;
                    if (!Constant.TsBaseType.Contains(propertyArrayItemType))
                    {
                        typeNames.Add(propertyArrayItemType);
                    }

                    if (Constant.UtilitiesModules.Contains(propertyArrayItemType))
                    {
                        nswagTypes.Add(propertyArrayItemType);
                    }
                }

                typeNames.Distinct().Where(c => !nswagTypes.Contains(c)).Where(c => c != typeName).ToList()
                    .ForEach(c => builder.AppendLine($"import {{ {c} }} from './{c}';"));
                if (nswagTypes.Any())
                {
                    builder.AppendLine($"import {{ {string.Join(",", nswagTypes.Distinct())} }} from './Utilities';");
                }

                builder.AppendLine();

                var template = _resolver.Settings.TemplateFactory.CreateTemplate("TypeScript", "Class", model);

                className = model.ClassName;

                return string.Join("\n", builder.ToString(),
                    template.Render(), appendCode);
            }
        }

        //private string GetDtoClassHeaderForImport(JsonSchema schema)
        //{
        //List<string> typeNames = new List<string>();
        //List<string> nswagTypes = new List<string>();
        //StringBuilder builder = new StringBuilder();
        //foreach (var actualProperty in schema.ActualProperties)
        //{
        //    if (actualProperty.Value.IsEnumeration && actualProperty.Value.Reference == null)
        //    {
        //        GenerateDtoClass(actualProperty.Value, actualProperty.Key, out _);
        //    }

        //    var property = new PropertyModel(model, actualProperty.Value, actualProperty.Key, _resolver,
        //        _resolver.Settings);

        //    var propertyType = property.Type.IndexOf("[", StringComparison.Ordinal) > 0
        //        ? property.Type.Replace("[]", "")
        //        : property.Type;
        //    if (!Constant.TsBaseType.Contains(propertyType) && !property.IsDictionary &&
        //        !actualProperty.Value.IsEnumeration)
        //    {
        //        typeNames.Add(propertyType);
        //    }

        //    if (Constant.UtilitiesModules.Contains(propertyType))
        //    {
        //        nswagTypes.Add(propertyType);
        //    }

        //    var propertyDictionaryItemType =
        //        property.DictionaryItemType.IndexOf("[", StringComparison.Ordinal) > 0
        //            ? property.DictionaryItemType.Replace("[]", "")
        //            : property.DictionaryItemType;
        //    if (!Constant.TsBaseType.Contains(propertyDictionaryItemType))
        //    {
        //        typeNames.Add(propertyDictionaryItemType);
        //    }

        //    if (Constant.UtilitiesModules.Contains(propertyDictionaryItemType))
        //    {
        //        nswagTypes.Add(propertyDictionaryItemType);
        //    }

        //    var propertyArrayItemType = property.ArrayItemType.IndexOf("[", StringComparison.Ordinal) > 0
        //        ? property.ArrayItemType.Replace("[]", "")
        //        : property.ArrayItemType;
        //    if (!Constant.TsBaseType.Contains(propertyArrayItemType))
        //    {
        //        typeNames.Add(propertyArrayItemType);
        //    }

        //    if (Constant.UtilitiesModules.Contains(propertyArrayItemType))
        //    {
        //        nswagTypes.Add(propertyArrayItemType);
        //    }
        //}

        //typeNames.Distinct()
        //    .Where(c => !nswagTypes.Contains(c))
        //    .Where(c => c != typeName)
        //    .ToList()
        //    .ForEach(c => builder.AppendLine($"import {{ {c} }} from './{c}';"));
        //if (nswagTypes.Any())
        //{
        //    builder.AppendLine($"import {{ {string.Join(",", nswagTypes.Distinct())} }} from './Utilities';");
        //}

        //builder.AppendLine();

        //var template = _resolver.Settings.CreateTemplate(typeName, model);

        //className = model.ClassName;

        //return string.Join("\n", builder.ToString();
        //}

        #endregion

        #region UtilitiesModule

        public void SetUtilitiesModuleName(string utilitiesModuleName)
        {
            _utilitiesModuleName = utilitiesModuleName;
        }

        /// <summary>
        /// for common function and dto
        /// </summary>
        /// <returns></returns>
        public string GenerateUtilities()
        {
            //var tempClientCode = "Placeholder Code For SwaggerException!";
            var tempClientCode = new List<CodeArtifact>();
            tempClientCode.Add(new CodeArtifact("tsException", CodeArtifactType.Undefined,
                CodeArtifactLanguage.TypeScript, CodeArtifactCategory.Undefined,
                "Placeholder Code For SwaggerException!"));
            var model = new TypeScriptFileTemplateModel(tempClientCode, new List<CodeArtifact>(), _openApiDocument,
                _extensionCode, _clientGeneratorSettings, _resolver);
            var template =
                _clientGeneratorSettings.CodeGeneratorSettings.TemplateFactory.CreateTemplate("TypeScript", "File",
                    model);
            var utilitiesCode = template.Render();
            utilitiesCode = utilitiesCode.Replace("function ", "export function ")
                .Replace("Placeholder Code For SwaggerException!", "");
            utilitiesCode = utilitiesCode.Replace("\n\n", "\n").Replace("\n\n", "\n").Replace("\n\n", "\n");
            return utilitiesCode;
        }

        #endregion

        /// <summary>
        /// Map ApiOperation to TypeScriptOperationModel
        /// </summary>
        /// <param name="openApiOperation"></param>
        /// <returns></returns>
        public virtual TypeScriptOperationModel GetOperationModelByApiOperation(
            OpenApiOperationDescription openApiOperation)
        {
            var operationModel =
                new TypeScriptOperationModel(openApiOperation.Operation, _clientGeneratorSettings,
                    _typeScriptClientGenerator, _resolver);

            operationModel.ControllerName = _clientGeneratorSettings.OperationNameGenerator.GetClientName(
                _openApiDocument, openApiOperation.Path, openApiOperation.Method, openApiOperation.Operation);
            operationModel.Path = openApiOperation.Path;
            operationModel.HttpMethod = openApiOperation.Method;
            operationModel.OperationName = _clientGeneratorSettings.OperationNameGenerator.GetOperationName(
                _openApiDocument, openApiOperation.Path, openApiOperation.Method, openApiOperation.Operation);
            return operationModel;
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
            return GetOperationModelsByPaths(_openApiDocument.Paths);
        }

        /// <summary>
        /// get operations by paths
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public virtual IEnumerable<TypeScriptOperationModel> GetOperationModelsByPaths(
            IDictionary<string, OpenApiPathItem> paths)
        {
            return paths.SelectMany(pair => GetOperationModelsByPaths(pair.Key, pair.Value));
        }

        /// <summary>
        /// Api path convert to operation model 
        /// </summary>
        /// <param name="apiPath"></param>
        /// <param name="pathItem"></param>
        /// <returns></returns>
        public virtual IEnumerable<TypeScriptOperationModel> GetOperationModelsByPaths(string apiPath,
            OpenApiPathItem pathItem)
        {
            return pathItem
                .Select(c => new
                {
                    Path = apiPath.TrimStart('/'),
                    HttpMethod = c.Key,
                    Operation = c.Value
                })
                .Select(c =>
                {
                    var operationModel =
                        new TypeScriptOperationModel(c.Operation, _clientGeneratorSettings,
                            _typeScriptClientGenerator, _resolver);

                    operationModel.ControllerName = _clientGeneratorSettings.OperationNameGenerator.GetClientName(
                        _openApiDocument, c.Path, c.HttpMethod, c.Operation);
                    operationModel.Path = c.Path;
                    operationModel.HttpMethod = c.HttpMethod;
                    operationModel.OperationName = _clientGeneratorSettings.OperationNameGenerator.GetOperationName(
                        _openApiDocument, c.Path, c.HttpMethod, c.Operation);
                    return operationModel;
                });
        }
    }
}