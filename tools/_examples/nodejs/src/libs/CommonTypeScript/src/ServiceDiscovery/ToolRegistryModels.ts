// Copyright (c) Microsoft. All rights reserved.

export enum ContentType {
    Json = "Json",
    Multipart = "Multipart"
}

export interface ToolDescription {
    Tool: string;
    Url: string;
    Method: string;
    InputType: ContentType;
    OutputType: ContentType;
    Description: string;
}
