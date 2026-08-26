using System.IO;
using NJsonSchema.CodeGeneration.TypeScript;
using NSwag.Commands;

using NSwagTsSplitter.Configuration;
using NSwagTsSplitter.Generators;
using NSwagTsSplitter.Helpers;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace NSwagTsSplitter.Tests.Generators;

public class UtilitiesGeneratorTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly GeneratorOption _generatorOption;
    private readonly NSwagDocument _nSwagDocument;

    public UtilitiesGeneratorTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        //Arrange
        var nswagPath = Path.Combine(Directory.GetCurrentDirectory(), "TestConfigs/service.config.nswag");
        _generatorOption = ArgumentsHelper.ReadArgs(new[] { "--config", nswagPath });
        _nSwagDocument = NsWagDocumentHelper.LoadDocumentFromFile(_generatorOption.ConfigPath);
    }

    [Fact]
    public void GenerateUtilitiesTest()
    {

        var resolver = new TypeScriptTypeResolver(_nSwagDocument.CodeGenerators.OpenApiToTypeScriptClientCommand
            .Settings.TypeScriptGeneratorSettings);
        UtilitiesGenerator utilitiesGenerator = new UtilitiesGenerator(
            _nSwagDocument.CodeGenerators.OpenApiToTypeScriptClientCommand.Settings,
            _generatorOption, resolver);

        // Act
        var result = utilitiesGenerator.Generate();

        // Assert
        result.Count.ShouldBe(4);
        _testOutputHelper.WriteLine(result[0].ModuleContent);
    }
}