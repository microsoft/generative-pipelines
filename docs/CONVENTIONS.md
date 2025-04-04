# Pipeline conventions

Let's consider the sample pipeline defined in [PIPELINES-INTRO.md](PIPELINES-INTRO.md):

```json
{
  "groupId": "A05",
  "region": "US",
  "_workflow": {
    "steps": [
      {
        "function": "users/search-users",
        "xin": "{ group: start.groupId }"
      },
      {
        "function": "users/search-addresses",
        "xin": "{ user_ids: users[].id, region: start.region }",
        "xout": "{ users: users[].{ id: id, name: name, address: addresses[?user_id == id].address } }"
      }
    ]
  }
}
```

## Tools

Function names are prefixed with `users/` because they belong to the **"users" tool**, which includes
multiple functions like `search-users` and `search-addresses`.

## HTTP Methods

By default, the orchestrator uses **POST to invoke functions**. Other HTTP methods are currently not supported.

## Mapping and Transformations

**Use `xin` to map and prepare function inputs, and `xout` to transform function outputs**.
Both parameters contain JMESPath expressions.

## Input Values

While values like `groupId` and `region` can be hardcoded, it’s better to pass them
as **input parameters** to support reuse and avoid duplication. Hardcoding values in JMESPath can also lead to syntax issues due to special characters.

Example with hardcoded values:

```json
{
  "_workflow": {
    "steps": [
      {
        "function": "users/search-users",
        "xin": "{ group: 'A05' }"
      },
      {
        "function": "users/search-addresses",
        "xin": "{ user_ids: users[].id, region: 'US' }",
        "xout": "{ users: users[].{ id: id, name: name, address: addresses[?user_id == id].address } }"
      }
    ]
  }
}
```

## Input Block Convention


A common (optional) convention is to place all input parameters under an `input` block. This helps with readability, especially in complex pipelines.

Example (notice the changes to the JMESPath expressions):

```json
{
  "input": {
    "groupId": "A05",
    "region": "US"
  },
  "_workflow": {
    "steps": [
      {
        "function": "users/search-users",
        "xin": "{ group: start.input.groupId }"
      },
      {
        "function": "users/search-addresses",
        "xin": "{ user_ids: users[].id, region: start.input.region }",
        "xout": "{ users: users[].{ id: id, name: name, address: addresses[?user_id == id].address } }"
      }
    ]
  }
}
```

## YAML Syntax

YAML improves readability and supports comments and multi-line JMESPath expressions.

```http request
POST <orchestrator>/api/jobs
Content-Type: application/x-yaml
```

```yaml
# Initial input
input:
  groupId: A05
  region: US

# The pipeline definition
_workflow:
  steps:
    # Step 1: get username and ID from the given group
    - function: users/search-users
      xin: >
        {
          group: start.input.groupId
        }
    # Step 2: get the address of a list of users
    - function: users/search-addresses
      xin: >
        {
          user_ids: users[].id,
          region:   start.input.region
        }
      # Step 3: combine the output of both functions
      xout: >
        {
          users: users[].{
            id:      id,
            name:    name,
            address: addresses[?user_id == id].address
          }
        }
```

# Next Read

Dive into [EXAMPLE-RAG-INGESTION.md](EXAMPLE-RAG-INGESTION.md) to learn more.
