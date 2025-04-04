// Copyright (c) Microsoft. All rights reserved.

namespace Orchestrator.Diagnostics;

internal static class Logging
{
    private static readonly string s_userProfileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static string RemovePiiFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) { return path ?? string.Empty; }

        return path.StartsWith(s_userProfileDir, StringComparison.OrdinalIgnoreCase)
            ? path.Replace(s_userProfileDir, "~", StringComparison.OrdinalIgnoreCase).TrimStart('/', '\\')
            : path;
    }

    public static string RemovePiiFromMessage(string? msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) { return msg ?? string.Empty; }

        return string.Join(' ', msg.Split(' ').Select(part =>
            part.StartsWith(s_userProfileDir, StringComparison.OrdinalIgnoreCase)
                ? part.Replace(s_userProfileDir, "~", StringComparison.OrdinalIgnoreCase).TrimStart('/', '\\')
                : part
        ));
    }

    // Old code using array of dirs to hide
    // internal string MaskDir(string? dir)
    // {
    //     if (dir == null || this.DevDirMasks == null || this.DevDirMasks.Count == 0)
    //     {
    //         return dir ?? string.Empty;
    //     }
    //
    //     var sortedKeys = this.DevDirMasks.Keys.OrderByDescending(key => key.Length);
    //     foreach (var prefix in sortedKeys)
    //     {
    //         if (dir.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    //         {
    //             return $"{this.DevDirMasks[prefix]}{dir[prefix.Length..]}";
    //         }
    //     }
    //
    //     return dir;
    // }
}
