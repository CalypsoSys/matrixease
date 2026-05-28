const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const test = require('node:test')

const repoRoot = path.resolve(__dirname, '..')

function read(relativePath) {
  return fs.readFileSync(path.join(repoRoot, relativePath), 'utf8')
}

function assertFile(relativePath) {
  assert.ok(fs.existsSync(path.join(repoRoot, relativePath)), `${relativePath} should exist`)
}

test('matrixease deployment assets are present', () => {
  [
    'scripts/matrixease/config.example.yaml',
    'scripts/matrixease/compose-matrixease.sh',
    'scripts/matrixease/matrixease.logrotate',
    'scripts/caddy/caddy.logrotate',
    'docker/matrixease/docker-compose.yml',
    'MatrixEase.Web/Dockerfile',
    '.gitleaks.toml',
    '.pre-commit-config.yaml',
    '.github/workflows/gitleaks.yml',
    'frontend/package.json',
    'frontend/functions/api/[[path]].ts',
    'frontend/public/robots.txt',
    'frontend/public/sitemap.xml',
    'docs/matrixease_local_vscode.md',
    'docs/cloudflare-pages-gateway.md',
    'docs/caddy_host_setup.md',
    'docs/matrixease_ubuntu_host_preparation.md',
    'docs/matrixease_production_runbook.md'
  ].forEach(assertFile)
})

test('example config records the agreed MatrixEase hosts, paths, and secrets', () => {
  const config = read('scripts/matrixease/config.example.yaml')

  assert.match(config, /MATRIXEASE_API_HOST_PORT: 8083/)
  assert.match(config, /MATRIXEASE_DATA_HOST_PATH: \/srv\/stacks\/matrixease\/data/)
  assert.match(config, /https:\/\/app\.matrixease\.com/)
  assert.match(config, /\$\{MATRIXEASE_GATEWAY_SECRET\}/)
  assert.match(config, /\$\{MATRIXEASE_SUPABASE_URL\}/)
  assert.match(config, /\$\{MATRIXEASE_SUPABASE_PUBLISHABLE_KEY\}/)
  assert.match(config, /\$\{MATRIXEASE_SLACK_FEEDBACK_WEBHOOK_URL\}/)
  assert.match(config, /MatrixEase:\n  Web:/)
})

test('compose wrapper renders YAML through babalu_yaml_env binary', () => {
  const script = read('scripts/matrixease/compose-matrixease.sh')

  assert.match(script, /DEFAULT_RENDER_BIN="\/srv\/utilities\/bin\/render-config-env"/)
  assert.match(script, /RENDER_BIN="\$\{RENDER_BIN:-\$DEFAULT_RENDER_BIN\}"/)
  assert.match(script, /\$SCRIPT_DIR\/render-config-env/)
  assert.match(script, /--format env/)
  assert.match(script, /docker compose/)
  assert.match(script, /COMPOSE_UNSET_ARGS/)
})

test('docker compose exposes the API on the agreed private port and mounts file storage', () => {
  const compose = read('docker/matrixease/docker-compose.yml')

  assert.match(compose, /container_name: matrixease-api/)
  assert.match(compose, /\$\{MATRIXEASE_API_HOST_PORT:-8083\}:8080/)
  assert.match(compose, /MatrixEase__Web__FileSaveLocation/)
  assert.match(compose, /MATRIXEASE_DATA_HOST_PATH:-\/srv\/stacks\/matrixease\/data/)
  assert.match(compose, /MATRIXEASE_LOGS_HOST_PATH:-\/srv\/logs\/matrixease\/api/)
})

test('Dockerfile publishes API image without legacy web_blaster prebuild', () => {
  const dockerfile = read('MatrixEase.Web/Dockerfile')
  const csproj = read('MatrixEase.Web/MatrixEase.Web.csproj')

  assert.match(dockerfile, /ENTRYPOINT \["dotnet", "MatrixEase\.Web\.dll"\]/)
  assert.doesNotMatch(csproj, /web_blaster/)
  assert.doesNotMatch(csproj, /SkipWebBlaster/)
})

test('gitleaks guardrails are configured for local and CI scans', () => {
  const config = read('.gitleaks.toml')
  const precommit = read('.pre-commit-config.yaml')
  const workflow = read('.github/workflows/gitleaks.yml')

  assert.match(config, /useDefault = true/)
  assert.match(precommit, /github\.com\/gitleaks\/gitleaks/)
  assert.match(workflow, /gitleaks\/gitleaks-action@v2/)
  assert.match(workflow, /GITLEAKS_CONFIG: \.gitleaks\.toml/)
})

test('VS Code backend launch renders MatrixEase API env file', () => {
  const launch = read('.vscode/launch.json')
  const tasks = read('.vscode/tasks.json')
  const gitignore = read('.gitignore')

  assert.match(launch, /Backend: MatrixEase\.Web \(no browser\)/)
  assert.match(launch, /\.vscode\/matrixease-api\.env/)
  assert.match(tasks, /backend: render local env/)
  assert.match(tasks, /scripts\/matrixease\/config\.local\.yaml/)
  assert.match(tasks, /\/srv\/utilities\/bin\/render-config-env/)
  assert.match(tasks, /scripts\/matrixease\/render-config-env/)
  assert.match(gitignore, /\.vscode\/matrixease-api\.env/)
  assert.match(gitignore, /scripts\/matrixease\/config\.local\.yaml/)
})

test('frontend scaffold uses Vite and Cloudflare Pages API gateway', () => {
  const packageJson = read('frontend/package.json')
  const indexHtml = read('frontend/index.html')
  const viteConfig = read('frontend/vite.config.ts')
  const gateway = read('frontend/functions/api/[[path]].ts')
  const robots = read('frontend/public/robots.txt')
  const sitemap = read('frontend/public/sitemap.xml')

  assert.match(packageJson, /"vite"/)
  assert.match(packageJson, /"vue"/)
  assert.match(packageJson, /"pinia"/)
  assert.match(indexHtml, /<link rel="canonical" href="https:\/\/app\.matrixease\.com\/"/)
  assert.match(indexHtml, /<meta property="og:title" content="MatrixEase"/)
  assert.match(indexHtml, /<meta name="twitter:card" content="summary"/)
  assert.match(robots, /Sitemap: https:\/\/app\.matrixease\.com\/sitemap\.xml/)
  assert.match(sitemap, /<loc>https:\/\/app\.matrixease\.com\/<\/loc>/)
  assert.match(viteConfig, /port: 5173/)
  assert.match(viteConfig, /VITE_API_PROXY_TARGET/)
  assert.match(gateway, /API_BASE_URL/)
  assert.match(gateway, /INTERNAL_API_KEY/)
})

test('docs describe api.matrixease.com and app.matrixease.com topology', () => {
  const gateway = read('docs/cloudflare-pages-gateway.md')
  const caddy = read('docs/caddy_host_setup.md')
  const runbook = read('docs/matrixease_production_runbook.md')

  assert.match(gateway, /app\.matrixease\.com/)
  assert.match(gateway, /api\.matrixease\.com/)
  assert.match(caddy, /reverse_proxy 127\.0\.0\.1:8083/)
  assert.match(runbook, /\/srv\/stacks\/matrixease\/data/)
  assert.match(runbook, /MATRIXEASE_SLACK_FEEDBACK_WEBHOOK_URL/)
})
