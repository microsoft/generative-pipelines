// Copyright (c) Microsoft. All rights reserved.

import { Post, Route, Tags, Body, Controller } from "tsoa";
import { User, Account } from "./models";
import { registerFunction } from "./libs/CommonTypeScript/src";


@Route("users")
@Tags("Users")
export class UserController extends Controller {

    @Post("/")
    public async createUser(@Body() user: User): Promise<Account> {
        return {
            id: Math.floor(Math.random() * 1000), // Simulated user ID
            username: user.name.toLowerCase().replace(/\s/g, "_"),
            email: user.email
        };
    }
}

// Register each endpoint as a function in the orchestrator's registry
(async () => registerFunction("/users", "POST", true, "Sample function"))().catch(console.error);
