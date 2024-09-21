using NSwagTsSplitter.Configuration;
using NSwagTsSplitter.Helpers;

using System.IO;

using Xunit;
using Xunit.Abstractions;

namespace NSwagTsSplitter.Tests.Configuration;

public class GeneratorOptionUtilTests
{
    private readonly GeneratorOption _generatorOption;
    private readonly ITestOutputHelper _testOutputHelper;
    public GeneratorOptionUtilTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var nswagPath = Path.Combine(Directory.GetCurrentDirectory(), "TestConfigs/service.config.nswag");
        _generatorOption = ArgumentsHelper.ReadArgs(new[] { "--config", nswagPath });
    }

    [Fact]
    public void GetKeyValueTypeTest()
    {
        var keyValueTypes = _generatorOption.GetKeyValueType("{ [key: Category]: Pet; }");
        foreach (var keyValueType in keyValueTypes)
        {
            _testOutputHelper.WriteLine($"{keyValueType.Key}:{keyValueType.Value}");
        }
    }
}