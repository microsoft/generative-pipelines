// Copyright (c) Microsoft. All rights reserved.

namespace Orchestrator.Config;

internal sealed class AppConfig
{
    public WebServiceAuthConfig Authorization { get; set; } = new();
    public WorkspaceConfig Workspace { get; set; } = new();

    public AppConfig Validate()
    {
        this.Workspace.Validate();
        this.Authorization.Validate();
        return this;
    }
}
