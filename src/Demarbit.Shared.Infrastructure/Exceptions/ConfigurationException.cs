namespace Demarbit.Shared.Infrastructure.Exceptions;

/// <summary>
/// Thrown when a required configuration value is missing or invalid.
/// </summary>
public class ConfigurationException : Exception
{
    /// <summary>
    /// The configuration key that is missing or invalid.
    /// </summary>
    public string ConfigurationKey { get; }

    /// <summary>
    /// Optional configuration section path for additional context.
    /// </summary>
    public string? SectionPath { get; }

    /// <summary>
    /// Creates an instance of the <see cref="ConfigurationException" /> class
    /// </summary>
    /// <param name="configurationKey">The configuration key that is missing or invalid</param>
    public ConfigurationException(string configurationKey)
        : base($"Configuration property '{configurationKey}' is missing or invalid.")
    {
        ConfigurationKey = configurationKey;
    }

    /// <summary>
    /// Creates an instance of the <see cref="ConfigurationException" /> class
    /// </summary>
    /// <param name="configurationKey">The configuration key that is missing or invalid</param>
    /// <param name="sectionPath">The path to the section that is missing or invalid</param>
    public ConfigurationException(string configurationKey, string sectionPath)
        : base($"Configuration property '{configurationKey}' in section '{sectionPath}' is missing or invalid.")
    {
        ConfigurationKey = configurationKey;
        SectionPath = sectionPath;
    }
}