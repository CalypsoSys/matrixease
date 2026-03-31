# MatrixEase.Static

Shared Nuxt frontend for both public web access and the Electron desktop shell.

## Goals

- Replace the duplicated legacy static trees in `MatrixEase.Web/wwwroot`, `MatrixEase.App/wwwroot`, and `static.matrixease.wwwroot`.
- Move platform-specific behavior out of `overrides.js` and into runtime config plus app adapters.
- Keep `MatrixEase.Web` focused on HTTP APIs for `api.matrixease.com`.
- Allow Electron to load the same built UI against an embedded local API.

## Scripts

- `npm run dev:web`
- `npm run dev:electron`
- `npm run generate:web`
- `npm run generate:electron`

## Runtime config

The app reads these public environment variables:

- `NUXT_PUBLIC_MATRIXEASE_PLATFORM`
- `NUXT_PUBLIC_MATRIXEASE_API_BASE`
- `NUXT_PUBLIC_MATRIXEASE_DOCS_BASE`
- `NUXT_PUBLIC_MATRIXEASE_MARKETING_BASE`
- `NUXT_PUBLIC_MATRIXEASE_EMBEDDED_API`

## Migration intent

This scaffold is intentionally small. The next steps are to port the current authentication, upload, matrix viewer, and reporting flows into composables and Nuxt UI pages while the existing C# endpoints are normalized for the new client.
