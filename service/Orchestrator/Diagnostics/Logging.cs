// Copyright (c) Microsoft. All rights reserved.

using Orchestrator.Config;

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

    public static void LogWorkspaceDetails(this ILogger log, WorkspaceConfig workspaceConfig, IConfiguration config, string blobStorageName)
    {
        if (workspaceConfig.UseFileSystem)
        {
            log.LogInformation("Starting Orchestrator, workspace on disk: {WorkspaceDir}", RemovePiiFromPath(workspaceConfig.WorkspaceDir));
        }
        else
        {
            string blobInfo = "";
            Dictionary<string, string>? connectionStrings = config.GetSection("ConnectionStrings").Get<Dictionary<string, string>>();
            string? cs = connectionStrings?.GetValueOrDefault(blobStorageName, "-");
            if (cs != null && cs.Contains("BlobEndpoint=", StringComparison.OrdinalIgnoreCase))
            {
                blobInfo = cs.Split(';').FirstOrDefault(x => x.StartsWith("BlobEndpoint=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1] ?? string.Empty;
            }
            else if (cs != null && cs.Contains("AccountKey=", StringComparison.OrdinalIgnoreCase))
            {
                blobInfo = cs.Split(';').FirstOrDefault(x => x.StartsWith("AccountName=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1] ?? string.Empty;
            }
            else if (cs != null && cs.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                blobInfo = cs;
            }

            log.LogInformation("Starting Orchestrator, workspace on blob storage: {BlobStorageDetails}", blobInfo);
        }
    }
}
