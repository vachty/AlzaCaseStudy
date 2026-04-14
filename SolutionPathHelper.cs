using System;

internal static class SolutionPathHelper
{
    public static string GetSolutionDirectory(string solutionFileName = "AlzaCaseStudy.slnx")
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, solutionFileName)))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                $"Could not locate solution root containing '{solutionFileName}'.");
    }
}
