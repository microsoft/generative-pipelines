// Copyright (c) Microsoft. All rights reserved.

import { Router, Request, Response } from "express";

interface TInput {
    name: string;
    age: number;
}

interface TOutput {
    message: string;
    isAdult: boolean;
}

const router = Router();

const getHome = (req: Request<{}, {}, TInput>, res: Response<TOutput>) => {
    const {name, age} = req.body;

    const response: TOutput = {
        message: `Hello, ${name}!`,
        isAdult: age >= 18,
    };

    res.json(response);
};

router.post("/", getHome);

export default router;
