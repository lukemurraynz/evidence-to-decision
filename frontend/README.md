# Evidence to Decision — Frontend

Browser-based evidence capture and review interface for the AI Opportunity Engineering workshop system. Facilitators capture attributed evidence, decision reviewers assess readiness, executives review outcomes, and delivery leads generate documents, all from a single hash-routed SPA backed by the Opportunity Engineering API.

See [PRODUCT.md](./PRODUCT.md) for the product context and user journeys, and [DESIGN.md](./DESIGN.md) for design direction and token system.

## Prerequisites

- Node.js `>=22.12.0` (Node.js 22 LTS or later)
- npm `>=10.9.8` (bundled with Node.js 22)

## Install

```sh
npm install
```

## Develop

```sh
npm run dev
```

The app is served at `http://localhost:5173`. It requests `/config.json` from the page origin before opening API-dependent views. During local development, a `404` on `/config.json` causes the app to use the page origin as the API origin, so no config file is required for simple setups.

## Build

```sh
npm run build
```

Runs `tsc -b` (type check) followed by `vite build`. Output goes to `dist/`.

## Lint

```sh
npm run lint
```

## Type check

```sh
npm run typecheck
```

## Test

```sh
npm test
```

Runs the Vitest test suite once. Use `npm run test:watch` for watch mode.

## Runtime configuration

Place a `config.json` file at the web root (served alongside `index.html`) with any non-secret overrides:

```json
{
  "apiBaseUrl": "https://workshop.example/",
  "requestTimeoutMs": 15000,
  "pollMaxAttempts": 20,
  "pollMaxElapsedMs": 180000
}
```

All fields are optional. When the file is absent the app uses the page origin as the API base URL and falls back to built-in defaults for the other values.

## Workspace connection

Open the app, then append `?workspace=<workspace-reference>&engagement=<engagement-reference>` to the URL to connect an engagement. The app does not persist these values in browser storage.
