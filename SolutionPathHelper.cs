using System;

/// <summary>
/// Provides path helper methods for locating the solution directory
/// Is suitable only for this case study as it relies on the solution file name and the directory structure of the project
/// Also is not the best practice and real scenario would require more robust solution
/// </summary>
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
