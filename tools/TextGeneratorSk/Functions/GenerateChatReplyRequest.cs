// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace TextGeneratorSk.Functions;

internal sealed class GenerateChatReplyRequest : IValidatableObject
{
    public GenerateChatReplyRequest FixState()
    {
        return this;
    }

    /// <inherit />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return ArraySegment<ValidationResult>.Empty;
    }
}
