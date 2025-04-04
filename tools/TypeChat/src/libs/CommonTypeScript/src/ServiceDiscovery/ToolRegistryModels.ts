// Copyright (c) Microsoft. All rights reserved.

export enum ContentType {
    Json = "Json",
    Multipart = "Multipart"
}

export interface FunctionDescription {
    Id: string;
    Tool: string;
    Url: string;
    Method: string;
    InputType: ContentType;
    OutputType: ContentType;
    Description: string;
}
