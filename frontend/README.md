# frontend

Vue 3 + TypeScript + Vite SPA for MatrixEase.

## Structure

- `src/pages/`: routed screens
- `src/layouts/`: shared application shell
- `src/components/`: reusable UI pieces
- `src/composables/`: page-level state and orchestration
- `src/services/`: API and runtime adapters
- `src/stores/`: Pinia state
- `src/utils/`: pure helpers
- `src/assets/css/`: theme and global styles

## Commands

- `npm --prefix frontend run dev`
- `npm --prefix frontend run dev:electron`
- `npm --prefix frontend run build`
- `npm --prefix frontend run test`

## Runtime config

The app reads these public environment variables:

- `VITE_MATRIXEASE_PLATFORM`
- `VITE_MATRIXEASE_API_BASE`
- `VITE_MATRIXEASE_DOCS_BASE`
- `VITE_MATRIXEASE_MARKETING_BASE`
- `VITE_MATRIXEASE_EMBEDDED_API`

## Migration intent

The app now follows the standard `frontend/src/...` SPA layout with a shared shell, routed pages, reusable components, a thin API layer, and Pinia state. Additional legacy viewer interactions can be ported into this structure without changing the host `MatrixEase.Web` API project.
