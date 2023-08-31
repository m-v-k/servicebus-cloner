using System;
using System.Reflection;
using CommandLine;
using Microsoft.Extensions.Logging;

public static class Utilities
{
    public static void SaveStringToFile(string content, string filePath, ILogger logger = null)
    {
        // Ensure the directory exists
        string directoryPath = Path.GetDirectoryName(filePath);
        if (directoryPath != null && !Directory.Exists(directoryPath))
        {
            logger.LogDebug($"Creating path '{directoryPath}'");
            Directory.CreateDirectory(directoryPath);
            logger.LogDebug($"Created path '{directoryPath}'");
        }

        // Write the content to the file (This will create the file if it doesn't exist or overwrite if it does)
        logger.LogDebug($"Writing to file '{filePath}'");
        File.WriteAllText(filePath, content);
        logger.LogDebug($"Written to file '{filePath}'");
    }

    public static LogLevel GetLogLevel(string[] args)
    {
        var levels = Enum.GetNames<LogLevel>();
        foreach(var level in levels)
        {
            if (args.Contains($"-l={level}") || args.Contains($"--loglevel={level}") || Environment.GetEnvironmentVariable("SC_LOGLEVEL") == level)
            {
                return Enum.Parse<LogLevel>(level);
            }
        }
        // default
        return LogLevel.Information;
    }

    public static string[] AppendEnvironmentVariables(string[] args, ILogger logger = null)
    {
        var options = GetOptions();

        var newArgs = new List<string>(args);
        foreach (var unusedOption in FilterOptions(args, options))
        {
            // repalced this 'Assembly.GetExecutingAssembly().GetName().Name.ToUpperInvariant()' with a static prefix of 'SC_'
            // ignore verbs since this solution has only 1
            var value = Environment.GetEnvironmentVariable($"SC_{unusedOption.LongName.ToUpperInvariant()}");
            
            // try reading a docker secret, ignore errors
            if (string.IsNullOrWhiteSpace(value))
            {
                logger.LogDebug($"Trying to read docker secret for {unusedOption.LongName} at /run/secrets/SC_{unusedOption.LongName.ToUpperInvariant()}");
                try
                {
                    value = File.ReadAllText($"/run/secrets/SC_{unusedOption.LongName.ToUpperInvariant()}");
                }
                catch{}
            }

            if (value != null)
            {
                newArgs.Add($"--{unusedOption.LongName}={value}");
            }
        }
        return newArgs.ToArray();
    }

    private static OptionAttribute[] GetOptions()
    {
        return typeof(Options).GetProperties()
            .Select(property => property.GetCustomAttributes<OptionAttribute>().FirstOrDefault())
            .Where(option => option != null).ToArray();
    }

    private static OptionAttribute[] FilterOptions(string[] args, OptionAttribute[] options)
    {
        var usedLongNames = new HashSet<string>();
        var usedShortNames = new HashSet<string>();

        foreach (var arg in args)
        {
            if (arg.StartsWith("--"))
            {
                var longName = arg.Substring(2);
                if (longName.Contains('='))
                {
                    longName = longName.Substring(0, longName.IndexOf('='));
                }
                usedLongNames.Add(longName);
            }
            else if (arg.StartsWith("-"))
            {
                var shortName = arg.Substring(1);
                if (shortName.Contains('='))
                {
                    shortName = shortName.Substring(0, shortName.IndexOf('='));
                }
                usedShortNames.Add(shortName);
            }
        }

        return options.Where(option => !usedLongNames.Contains(option.LongName) && !usedShortNames.Contains(option.ShortName)).ToArray();
    }

}
