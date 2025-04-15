// Copyright (c) Microsoft. All rights reserved.

namespace Orchestrator.Config;

internal sealed class WorkspaceConfig
{
    private static readonly string[] s_defaultWorkspace = ["generative-pipelines", "data", "workspace"];
    private static readonly string s_userProfileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public bool UseFileSystem { get; set; } = false;

    public string WorkspaceDir { get; set; } = string.Empty;

    public WorkspaceConfig Validate()
    {
#pragma warning disable IDE0055
        this.WorkspaceDir = string.IsNullOrWhiteSpace(this.WorkspaceDir)
            ? Path.Join([s_userProfileDir, ..s_defaultWorkspace])
            : this.WorkspaceDir.Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar);
#pragma warning restore IDE0055
        return this;
    }
}
