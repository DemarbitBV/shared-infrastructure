using Demarbit.Shared.Infrastructure.Exceptions;

namespace Demarbit.Shared.Infrastructure.Tests.Exceptions;

public class ConfigurationExceptionTests
{
    [Fact]
    public void Constructor_WithKeyOnly_SetsMessageAndProperties()
    {
        var exception = new ConfigurationException("MyKey");

        Assert.Equal("MyKey", exception.ConfigurationKey);
        Assert.Null(exception.SectionPath);
        Assert.Equal("Configuration property 'MyKey' is missing or invalid.", exception.Message);
    }

    [Fact]
    public void Constructor_WithKeyAndSection_SetsMessageAndProperties()
    {
        var exception = new ConfigurationException("MyKey", "MySection");

        Assert.Equal("MyKey", exception.ConfigurationKey);
        Assert.Equal("MySection", exception.SectionPath);
        Assert.Equal("Configuration property 'MyKey' in section 'MySection' is missing or invalid.", exception.Message);
    }

    [Fact]
    public void ConfigurationException_InheritsFromException()
    {
        var exception = new ConfigurationException("Key");

        Assert.IsAssignableFrom<Exception>(exception);
    }
}
