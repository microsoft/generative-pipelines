# Software required

List of software required for local execution:

## Languages

- [**.NET SDK v9**](https://dotnet.microsoft.com/download/dotnet/9.0).
- **Node.js v20** or later. Recommended using [nvm](https://github.com/nvm-sh/nvm) to manage Node.js versions.
- **Python 3.11** or later.

## Tools

- [**Just**](https://github.com/casey/just), used for command line tasks, e.g. building the solution.
- [**Poetry**](https://python-poetry.org/docs/#installation), used for Python packages/build/venv.
- [**pnpm**](https://pnpm.io/installation), used for Node.js packages and builds.
- [Docker Desktop](https://www.docker.com/products/docker-desktop) or [Podman](https://podman.io),
   used to run some services and to package resources during deployments. If you use Podman,
   [check this .NET Aspire documentation](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling).

## External Services

- You can create pipelines without any external dependency, e.g. using Ollama, Postgres,
  Docker images, etc. However, to easily deploy to the cloud or to use any Azure service,
  you'll need an Azure subscription.
  You can create a free account [here](https://azure.microsoft.com/free).
- **Azure OpenAI** or **OpenAI** is used for text and embedding generation. Other models
    can be added by developing custom tools.
- **Azure AI Search** is used for vector search. Other options are available, such as **Qdrant**
    and **Postgres**, or can be added by developing custom tools.
- **Azure Document Intelligence** is used for document processing. Other options will be available
    or can be added by developing custom tools.

## Deployment

You will need this only if you choose to deploy the solution to Azure.

- An Azure subscription, you can create a free account [here](https://azure.microsoft.com/free).
- Install [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) and
  [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd).

Test your Azure CLI installation by running:

Current account:

```shell
az account show
```

If needed, sign in:

```shell
az login
```

## Optional

- [VS Code](https://code.visualstudio.com/) with
  [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension
  or [Visual Studio 2022](https://visualstudio.microsoft.com)
- [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/)
  can help browsing pipelines' data stored in the local emulator and in the cloud.
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) is automatically
  installed when using .NET Aspire. It can also be installed manually, if needed.
- [Bruno](https://www.usebruno.com) is an HTTP client that can be used to test the APIs. It supports
  collections, variables, and other features that can help testing the APIs. A few Bruno collections
  are available in the `examples` folder.
