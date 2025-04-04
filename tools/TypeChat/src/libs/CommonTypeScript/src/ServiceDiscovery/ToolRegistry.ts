// Copyright (c) Microsoft. All rights reserved.

import Redis from "ioredis";
import {ContentType, FunctionDescription} from "./ToolRegistryModels";
import {log} from "../../../CommonTypeScript/src";


let redis: Redis | null = null;

/**
 * Returns a cached Redis client instance.
 * If not already created, it parses the env var and creates a new client.
 */
export function getRedisClient(): Redis | null {
    if (!redis) {
        // Format: "localhost:57033", injected by Aspire
        const connStr = process.env["ConnectionStrings__redis-storage"] || "";
        log.info("Redis connection string: ", {connString: connStr});
        if (!connStr) {
            log.warn("Redis connection string not found in environment variables.");
            return null;
        }
        const [host, port] = connStr.split(":");
        redis = new Redis(parseInt(port, 10), host);
        log.info("Redis client ready");
    }
    return redis;
}

export async function registerFunction(
    url: string,
    method: string,
    isJson: boolean,
    description: string
) {

    const redis = getRedisClient();
    if (!redis) {
        return;
    }

    // The tool name should be set using an env var, e.g. injected by the hosting environment
    const toolName: string = process.env.TOOL_NAME || "";

    const data: FunctionDescription = {
        Id: `${toolName}${url}`,
        Tool: toolName,
        Url: url,
        Method: method.toString(),
        InputType: isJson ? ContentType.Json : ContentType.Multipart,
        OutputType: ContentType.Json,
        Description: description,
    };

    // Store a unique ID into a "functions" Redis set, used to index key-values.
    // The ID points to a Redis key where the entire function description is stored.
    // This approach allows to modify function details without causing
    // duplicate entries in the Redis set.

    // Data stored in Redis KV
    const redisDataKey = `FunctionDetails:${toolName}:${url}`
    const redisDataValue = JSON.stringify(toCamelCase(data))
    await redis.set(redisDataKey, redisDataValue);

    // Pointer stored in Redis Set
    await redis.sadd("functions", redisDataKey);
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
