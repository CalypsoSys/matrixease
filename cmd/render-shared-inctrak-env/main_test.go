package main

import (
	"os"
	"strings"
	"testing"
)

func TestRenderEnvFileFlattensNestedSettings(t *testing.T) {
	lines, err := renderEnvFile([]byte(`
SHARED_API_IMAGE: shared-inctrak-api:latest
AppSettings:
  AllowedOrigins:
    - https://shared.inctrak.com
    - https://localhost:3000
  RequireGatewaySecret: true
  GatewaySecretHeaderName: X-Internal-Api-Key
`))
	if err != nil {
		t.Fatalf("renderEnvFile returned error: %v", err)
	}

	rendered := strings.Join(lines, "\n")
	assertContains(t, rendered, "SHARED_API_IMAGE=shared-inctrak-api:latest")
	assertContains(t, rendered, "AppSettings__AllowedOrigins__0=https://shared.inctrak.com")
	assertContains(t, rendered, "AppSettings__AllowedOrigins__1=https://localhost:3000")
	assertContains(t, rendered, "AppSettings__RequireGatewaySecret=true")
	assertContains(t, rendered, "AppSettings__GatewaySecretHeaderName=X-Internal-Api-Key")
}

func TestRenderEnvFileRequiresEnvironmentVariable(t *testing.T) {
	t.Setenv("GOOGLE_CLIENT_SECRET", "")
	if err := os.Unsetenv("GOOGLE_CLIENT_SECRET"); err != nil {
		t.Fatalf("Unsetenv failed: %v", err)
	}

	_, err := renderEnvFile([]byte(`
AppSettings:
  GoogleClientSecret: ${GOOGLE_CLIENT_SECRET}
`))
	if err == nil {
		t.Fatal("renderEnvFile succeeded with a missing required environment variable")
	}

	assertContains(t, err.Error(), "required environment variable is not set: GOOGLE_CLIENT_SECRET")
}

func TestRenderShellExportsResolvesEmbeddedPlaceholders(t *testing.T) {
	t.Setenv("GATEWAY_SECRET", "gateway-secret")
	t.Setenv("FILE_ROOT", "/srv/shared-inctrak")

	lines, err := renderShellExports([]byte(`
AppSettings:
  GatewaySecret: ${GATEWAY_SECRET}
  FileSaveLocation: ${FILE_ROOT}/mangadata
`))
	if err != nil {
		t.Fatalf("renderShellExports returned error: %v", err)
	}

	rendered := strings.Join(lines, "\n")
	assertContains(t, rendered, "export AppSettings__GatewaySecret='gateway-secret'")
	assertContains(t, rendered, "export AppSettings__FileSaveLocation='/srv/shared-inctrak/mangadata'")
}

func assertContains(t *testing.T, actual string, expected string) {
	t.Helper()
	if !strings.Contains(actual, expected) {
		t.Fatalf("expected %q to contain %q", actual, expected)
	}
}
