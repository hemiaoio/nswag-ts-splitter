using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NSwag;
using NSwag.Commands;

using NSwagTsSplitter.Generators;
using NSwagTsSplitter.Helpers;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace NSwagTsSplitter.Tests
{
    public class TypeScriptGenerateTests
    {
        private readonly SwaggerDocumentHelper _swaggerDocumentHelper;
        private readonly ClientsScriptGenerator _selfTypeScriptGenerator;
        private readonly UtilitiesScriptGenerator _utilsScriptGenerator;
        private readonly ModelsScriptGenerator _modelsScriptGenerator;
        private readonly ITestOutputHelper _outputHelper;
        private readonly OpenApiDocument _openApiDocument;

        public TypeScriptGenerateTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _swaggerDocumentHelper = new SwaggerDocumentHelper();
            var nSwagDocument = LoadSettings().Result;
            _openApiDocument = LoadOpenApi().Result;
            var settings = nSwagDocument.CodeGenerators
                .OpenApiToTypeScriptClientCommand.Settings;
            _selfTypeScriptGenerator = new ClientsScriptGenerator(settings, _openApiDocument);
            _utilsScriptGenerator = new UtilitiesScriptGenerator(settings, _openApiDocument);
            _modelsScriptGenerator = new ModelsScriptGenerator(settings, _openApiDocument);
        }

        protected async Task<NSwagDocument> LoadSettings()
        {
            var configFilePath = Path.Combine(AppContext.BaseDirectory, "./Config/nswag.nswag");
            var nSwagDocument = await NSWagDocumentHelper.LoadDocumentFromFileAsync(configFilePath);

            nSwagDocument.ShouldNotBeNull();
            nSwagDocument.CodeGenerators.ShouldNotBeNull();
            nSwagDocument.CodeGenerators.OpenApiToTypeScriptClientCommand.ShouldNotBeNull();
            nSwagDocument.CodeGenerators.OpenApiToTypeScriptClientCommand.Settings.ShouldNotBeNull();
            return nSwagDocument;
        }

        protected async Task<OpenApiDocument> LoadOpenApi()
        {
            var swaggerFilePath = Path.Combine(AppContext.BaseDirectory, "./Config/swagger.json");
            var openApiDocument = await _swaggerDocumentHelper.FromPathAsync(swaggerFilePath);
            return openApiDocument;
        }

        #region ClientClass

        [Fact]
        public void GenerateClientClass_Test()
        {
            _selfTypeScriptGenerator.SetDtoPath("Dto");
            var classCode = _selfTypeScriptGenerator.GenerateClientClass("Account");
            classCode.ShouldNotBeNullOrWhiteSpace();
            classCode.ShouldContain("IAccountServiceProxy");
            _outputHelper.WriteLine(classCode);
        }

        [Fact]
        public void GenerateClientClassWithOperationModels_Test()
        {
            var operationModels = _openApiDocument.Operations.Take(10)
                .Select(c => _selfTypeScriptGenerator.GetOperationModelByApiOperation(c));
            var classCode =
                _selfTypeScriptGenerator.GenerateClientClassWithOperationModels("Demo", operationModels);
            classCode.ShouldNotBeNullOrWhiteSpace();
            classCode.ShouldContain("IDemoServiceProxy");
            _outputHelper.WriteLine(classCode);
        }

        [Fact]
        public void GenerateClientClasses_Test()
        {
            var outputDirectory = Path.Combine(AppContext.BaseDirectory, "client");
            var clientClasses = _selfTypeScriptGenerator.GenerateClientClasses();
            clientClasses.ToList().Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void GenerateClientClassWithApiOperations_Test()
        {
            var options = _openApiDocument.Operations.Take(10);
            var classCode = _selfTypeScriptGenerator.GenerateClientClassWithApiOperations("Demo", options);
            classCode.ShouldNotBeNullOrWhiteSpace();
            classCode.ShouldContain("IDemoServiceProxy");
            _outputHelper.WriteLine(classCode);
        }

        [Fact]
        public void GenerateClientClassWithNameAndOperations_Test()
        {
            var options = _openApiDocument.Operations.Take(10)
                .Select(c => _selfTypeScriptGenerator.GetOperationModelByApiOperation(c));
            var classCode =
                _selfTypeScriptGenerator.GenerateClientClassWithNameAndOperations("Demo", "AbcServiceProxy", options);
            classCode.ShouldNotBeNullOrWhiteSpace();
            classCode.ShouldContain("IAbcServiceProxy");
            _outputHelper.WriteLine(classCode);
        }

        [Fact]
        public void GetClientClassHeaderForImport_Test()
        {
            var operationModels = _openApiDocument.Operations.Take(1)
                .Select(c => _selfTypeScriptGenerator.GetOperationModelByApiOperation(c));
            var headerCode = _selfTypeScriptGenerator.GetClientClassHeaderForImport(operationModels);
            headerCode.ShouldNotBeNullOrWhiteSpace();
            _outputHelper.WriteLine(headerCode);
        }

        #endregion

        #region UtilitiesModule

        [Fact]
        public void GenerateUtilities_Test()
        {
            var utilitiesCode = _utilsScriptGenerator.GenerateUtilities();
            _outputHelper.WriteLine(utilitiesCode);
            utilitiesCode.ShouldNotBeNullOrWhiteSpace();
        }

        #endregion

        #region DtoClass

        [Fact]
        public void GenerateDtoClasses_Test()
        {
            var result = _modelsScriptGenerator.GenerateDtoClasses();
            result.Count().ShouldBeGreaterThan(0);
        }

        [Fact]
        public void GenerateDtoClasses_ShouldBeImportParentType_Test()
        {
            var result = _modelsScriptGenerator.GenerateDtoClasses();
            var tags = result.Where(c => c.Key.StartsWith("Tag"));
            var tag = tags.FirstOrDefault(c => c.Key.Equals("TagDto"));
            var contains = tag.Value
                .Contains("import { ITagBasicDto,TagBasicDto } from './TagBasicDto';");
            contains.ShouldBe(true);
        }

        [Fact]
        public void GenerateDtoClasses_ShouldBeImportParentType_With_Signal_Test()
        {
            var result = _modelsScriptGenerator.GenerateDtoClasses();
            var tags = result.Where(c => c.Key.StartsWith("UserLogin"));
            var tag = tags.FirstOrDefault(c => c.Key.Equals("UserLoginAttemptDtoListResultDto"));
            tag.Value.Split("\r")[1].Replace("\n", "").ShouldBe(string.Empty);
            //.Contains("import { UserLoginAttemptDto } from './UserLoginAttemptDto';");
            //contains.ShouldBe(true);
        }

        [Fact]
        public void GenerateDtoClass_Test()
        {
            var index = new Random().Next(_openApiDocument.Definitions.Count);
            var schema = _openApiDocument.Definitions["ActivityDto"];
            var code = _modelsScriptGenerator.GenerateDtoClass(schema, "ActivityDto", out _);
            _outputHelper.WriteLine(code);
            code.ShouldNotBeNullOrWhiteSpace();
        }

        #endregion

        [Fact]
        public void GetAllOperations_Test()
        {
            var operations = _selfTypeScriptGenerator.GetAllOperationModels();
            operations.Count().ShouldBeGreaterThan(0);
        }
    }
}