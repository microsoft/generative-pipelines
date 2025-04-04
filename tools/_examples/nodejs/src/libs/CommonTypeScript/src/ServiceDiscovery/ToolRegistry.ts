// Copyright (c) Microsoft. All rights reserved.

import Redis from "ioredis";
import { ContentType, FunctionDescription } from "./ToolRegistryModels";

let redis: Redis | null = null;

/**
 * Returns a cached Redis client instance.
 * If not already created, it parses the env var and creates a new client.
 */
export function getRedisClient(): Redis | null {
    if (!redis) {
        // Format: "localhost:57033", injected by Aspire
        const connStr = process.env.ConnectionStrings__redis || "";
        if (!connStr) { return null; }
        const [host, port] = connStr.split(":");
        redis = new Redis(parseInt(port, 10), host);
    }
    return redis;
}

export async function registerTool(
    url: string,
    method: string,
    isJson: boolean,
    description: string
) {

    const redis = getRedisClient();
    if (!redis) { return; }

    const toolName: string = process.env.TOOL_NAME || "";

    const data: FunctionDescription = {
        Tool: toolName,
        Url: url,
        Method: method.toString(),
        InputType: isJson ? ContentType.Json : ContentType.Multipart,
        OutputType: ContentType.Json,
        Description: description,
    };

    await redis.sadd("tools", JSON.stringify(toCamelCase(data)));
}

function toCamelCase(obj: any): any {
    if (!obj || typeof obj !== 'object') return obj;
    if (Array.isArray(obj)) return obj.map(toCamelCase);

    return Object.keys(obj).reduce((acc, key) => {
        const camelKey = key[0].toLowerCase() + key.slice(1);
        acc[camelKey] = toCamelCase(obj[key]);
        return acc;
    }, {} as any);
}
