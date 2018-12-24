using System.Collections.Generic;
using Xunit;
using Amazon.Lambda.TestUtilities;
using Microsoft.Extensions.Configuration;

namespace NetCoreLambda.Tests
{
    public class FunctionTest
    {
        [Fact]
        public void Function_Should_Return_Config_Variable()
        {
            var expected = "val1";
            var configValues = new Dictionary<string, string> { ["env1"] = "val1" };
            var testConfig = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

            var function = new Function(testConfig);
            var result = function.FunctionHandler("env1", new TestLambdaContext());
            Assert.Equal(expected, result);
        }
    }
}
