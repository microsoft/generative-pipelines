// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;

namespace Orchestrator.Config;

internal sealed class AuthorizationConfig
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthType
    {
        None,
        AccessKey,
    }

    public AuthType Type { get; set; } = AuthType.None;
    public string AuthorizationHeader { get; set; } = string.Empty;
    public string AccessKey1 { get; set; } = string.Empty;
    public string AccessKey2 { get; set; } = string.Empty;

    public AuthorizationConfig Validate()
    {
        if (this.Type == AuthType.None) { return this; }

        this.AuthorizationHeader = string.IsNullOrWhiteSpace(this.AuthorizationHeader) ? "Authorization" : this.AuthorizationHeader.Trim();

        if (this.Type == AuthType.AccessKey)
        {
            const int MinLen = 16;
            char[] allowedSymbols = ['!', '$', '%', '(', ')', '*', '-', '.', ':', ';', '[', ']', '^', '_', '{', '|', '}', '~'];

            void ValidateKey(string value, string name)
            {
                if (value.Length < MinLen)
                {
                    throw new ApplicationException($"{name} must be at least 16 characters long");
                }

                if (!value.All(c => char.IsLetterOrDigit(c) || allowedSymbols.Contains(c)))
                {
                    throw new ApplicationException($"{name} contains invalid symbols. Allowed: letters, digits, and {string.Join(" ", allowedSymbols)}");
                }
            }

            this.AccessKey1 = string.IsNullOrWhiteSpace(this.AccessKey1) ? string.Empty : this.AccessKey1.Trim();
            this.AccessKey2 = string.IsNullOrWhiteSpace(this.AccessKey2) ? string.Empty : this.AccessKey2.Trim();

            ValidateKey(this.AccessKey1, nameof(this.AccessKey1));
            ValidateKey(this.AccessKey2, nameof(this.AccessKey2));
        }

        return this;
    }
}
