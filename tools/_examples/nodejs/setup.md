```shell
nvm use 22
pnpm init

pnpm add express
pnpm add -D typescript ts-node @types/node @types/express

pnpm exec tsc --init
```

change tsconfig.json to:

```json
{
  "compilerOptions": {
    "target": "ES6",
    "module": "CommonJS",
    "outDir": "./dist",
    "rootDir": "./src",
    "strict": true
  }
}
```

Create src/index.ts:

```typescript
import express, { Request, Response } from "express";

const app = express();
const PORT = process.env.PORT || 3000;

app.use(express.json());

app.get("/", (req: Request, res: Response) => {
  res.send("Hello, TypeScript Web Service with pnpm!");
});

app.listen(PORT, () => {
  console.log(`Server is running on http://localhost:${PORT}`);
});
```

Modify package.json scripts:

```json
"scripts": {
  "start": "pnpm exec ts-node src/index.ts"
}
```

# File upload

Multer is a middleware for handling multipart/form-data, which is used for file uploads.

```shell
pnpm add multer
pnpm add -D @types/multer
```

# TypeChat

```shell
pnpm add typechat
```

# Barrelsby

pnpm add -D barrelsby

Barrelsby is a tool for automatically generating TypeScript barrel files (index.ts)
that export modules from a directory. This makes imports cleaner and easier to manage.
Run this to generate/update the `src/libs/CommonTypeScript/src/index.ts` file:

```shell
npx barrelsby --config barrelsby.json
```