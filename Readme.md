# AWS Lambda Function with Dependency Injection and Configuration

Demonstrates how to add .NET Core dependency injection and configuration services to an AWS Lambda Function project for .NET Core 2.1.

## Summary

The idea behind this approach is to leverage the built-in configuration system of .NET Core, which can accept mulitple inputs that can override one another. This allows for use of an **appsettings.json** file with settings entries that can be overriden by environment variables applied when the Lambda Function is deployed.  This allows for values that will be available when debugging the function locally, as well as values that can be set as part of a CICD pipeline.

## Prerequisites

- Visual Studio 2017 or greater.
- AWS Toolkit for Visual Studio 2017.
- .NET Core 2.1 SDK

## Setup Steps

1. Open VS 2017 and create a new project.
    - Select **AWS Lambda Project with Tests**.

1. Set project SDK to "Microsoft.NET.Sdk.Web".

1. Add package reference to "Microsoft.AspNetCore.App"
    - Version="2.1.5"

1. Add an **appsettings.json** file to the project.
    - Add the following content:

    ```json
    {
        "env1": "val1",
        "env2": "val2",
        "env3": "val3"
    }
    ```

1. Add an **appsettings.Development.json** file to the project.
    - Add the following content:

    ```json
    {
        "env1": "dev-val1",
        "env2": "dev-val2",
        "env3": "dev-val3"
    }
    ```

1. Select both JSON files, open the Propeties window of Visual Studio, then set the `BuildAction` property to **Content** and the `Copy to Output Directory` property to **Copy always**. 

1. Open the **aws-lambda-tools-defaults.json** file and add the following:

    ```json
    "environment-variables" : "\"ASPNETCORE_ENVIRONMENT\"=\"Development\";\"env1\"=\"val1\";\"env2\"=\"val2\"",
    ```

1. Add an `environmentVariables` property to the **launchSettings.json** file.

    ```json
    "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
    }
    ```

1. Add a `Constants` class to the project.

    ```csharp
    public static class Constants
    {
        public static class EnvironmentVariables
        {
            public const string AspnetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
        }

        public static class Environments
        {
            public const string Production = "Production";
        }
    }
    ```

1. Add `DependencyResolver` class.

    ```csharp
    public class DependencyResolver
    {
        public IServiceProvider ServiceProvider { get; }

        public DependencyResolver(Action<IServiceCollection> registerServices)
        {
            // Set up Dependency Injection
            var services = new ServiceCollection();
            registerServices?.Invoke(services);
            ServiceProvider = services.BuildServiceProvider();
        }
    }
    ```

1. Add `ServiceCollectionExtensions` class.

    ```csharp
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfiguration(
            this IServiceCollection services,
            Func<IConfigurationBuilder, IConfiguration> builderFunc)
        {
            if (builderFunc == null)
                throw new ArgumentNullException(nameof(builderFunc));
            return services.AddTransient(provider => builderFunc(new ConfigurationBuilder()));
        }
    }
    ```

1. Add a `ConfigureServices` method to the `Function` class in order to register services with the DI system.

    ```csharp
    private void ConfigureServices(IServiceCollection services)
    {
        // Add Configuration to DI system
        services.AddConfiguration(builder =>
        {
            // Get ASPNETCORE_ENVIRONMENT
            var environment = Environment.GetEnvironmentVariable(
                Constants.EnvironmentVariables.AspnetCoreEnvironment)
                ?? Constants.Environments.Production;

            // Build config
            return builder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        });
    }
    ```

1. Add a property `IConfiguration` property and two constructors to the `Function` class.

    ```csharp
    // Configuration
    private IConfiguration Configuration { get; }

    public Function()
    {
        // Set up dependency resolution
        var resolver = new DependencyResolver(ConfigureServices);

        // Get Configuration from DI system
        Configuration = resolver.ServiceProvider.GetService<IConfiguration>();
    }

    // Use this ctor from unit tests that can mock IConfiguration
    public Function(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    ```

1. Flesh out the `FunctionHandler` method to use the `IConfiguration`.
    - Use the input parameter for the key used to retrieve a config setting.

    ```csharp
    public string FunctionHandler(string input, ILambdaContext context)
    {
        // Get config setting using input as a key
        return Configuration[input] ?? "None";
    }
    ```

## Local Debugging

1. Press F5 to launch the Mock Lambda Test Tool and start debugging.

1. Enter `"env1"` into Function Input, and click Execute Function.
    - A value of `"dev-val1"` should be returned base on the appsettings.Development.json file.

## Unit Testing

1. Add the NuGet package for **Moq** to the Test project.

1. Add a reference to the **NetCoreLambda** project.

1. Mock `IConfiguration` (from Microsoft.Extensions.Configuration) to return the expected value.
    - Invoke the lambda function and confirm config value is returned.

    ```csharp
    [Fact]
    public void Function_Should_Return_Config_Variable()
    {
        // Mock IConfiguration
        var expected = "val1";
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(p => p[It.IsAny<string>()]).Returns(expected);

        // Invoke the lambda function and confirm config value is returned
        var function = new Function(mockConfig.Object);
        var result = function.FunctionHandler("env1", new TestLambdaContext());
        Assert.Equal(expected, result);
    }
    ```

## Deployment

1. Right-click on the project in the Solutions Explorer and select Publish to AWS Lambda.

2. Under Configuration you can change the values for the environment variables, and these will override the values from the appsettings.json file published with the Lambda Function.

## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can deploy your application using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Execute unit tests
```
    cd "NetCoreLambda/test/NetCoreLambda.Tests"
    dotnet test
```

Deploy function to AWS Lambda
```
    cd "NetCoreLambda/src/NetCoreLambda"
    dotnet lambda deploy-function
```
