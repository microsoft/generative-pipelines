﻿// Copyright (c) Microsoft. All rights reserved.

namespace Aspire.AppHost.PatchedAddUvicornApp;

internal sealed class VirtualEnvironment(string virtualEnvironmentPath)
{
    /// <summary>
    /// Locates an executable in the virtual environment.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <returns>Returns the path to the executable if it exists in the virtual environment.</returns>
    public string? GetExecutable(string name)
    {
        if (OperatingSystem.IsWindows())
        {
            string[] allowedExtensions = [".exe", ".cmd", ".bat"];

            foreach (var allowedExtension in allowedExtensions)
            {
                string executablePath = Path.Join(virtualEnvironmentPath, "Scripts", name + allowedExtension);

                if (File.Exists(executablePath))
                {
                    return executablePath;
                }
            }
        }
        else
        {
            var executablePath = Path.Join(virtualEnvironmentPath, "bin", name);

            if (File.Exists(executablePath))
            {
                return executablePath;
            }
        }

        return null;
    }

    /// <summary>
    /// Locates a required executable in the virtual environment.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <returns>The path to the executable in the virtual environment.</returns>
    /// <exception cref="DistributedApplicationException">Gets thrown when the executable couldn't be located.</exception>
    public string GetRequiredExecutable(string name)
    {
        return this.GetExecutable(name) ?? throw new DistributedApplicationException(
            $"The executable {name} could not be found in the virtual environment at '{virtualEnvironmentPath}' . " +
            "Make sure the virtual environment is initialized and the executable is installed.");
    }
}
