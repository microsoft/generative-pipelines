// Copyright (c) Microsoft. All rights reserved.

import express from "express";
import homeRoutes from "./home.routes";

const app = express();

app.use(express.json());

app.use(homeRoutes);

export default app;
