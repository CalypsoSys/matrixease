# Frontend Modernization

## Direction

MatrixEase is moving to a single Vite-native frontend built with Vue 3, PrimeVue, Pinia, Vue Router, and Tailwind CSS.

## Target shape

- `frontend/` is the shared frontend source.
- `MatrixEase.Web/` becomes API-only for `api.matrixease.com`.
- `MatrixEase.App/` remains the Electron-hosted local API and desktop shell, but it should load the shared built frontend instead of keeping its own `wwwroot` application copy.

## Replacements

- `overrides.js` becomes runtime config and explicit platform adapters.
- legacy copy/minify build steps have been removed in favor of frontend-native builds.
- duplicated HTML/Vue-in-script pages become typed Vue single-file components.

## Immediate next migration steps

1. Install frontend dependencies and verify the Vite scaffold builds.
2. Add shared API composables for session, auth, uploads, and matrix data.
3. Port the current UI flow into routed Vue pages/components.
4. Remove static-file serving responsibilities from `MatrixEase.Web`.
5. Point Electron at the generated frontend output and local API runtime.
