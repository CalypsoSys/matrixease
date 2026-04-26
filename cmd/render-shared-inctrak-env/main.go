package main

import (
	"flag"
	"fmt"
	"os"
	"regexp"
	"sort"
	"strconv"
	"strings"

	"gopkg.in/yaml.v3"
)

var placeholderPattern = regexp.MustCompile(`\$\{([A-Za-z_][A-Za-z0-9_]*)\}`)

func main() {
	format := flag.String("format", "env", "Output format: env or shell")
	flag.Parse()

	if flag.NArg() != 1 {
		fmt.Fprintln(os.Stderr, "Usage: render-shared-inctrak-env --format env|shell /path/to/config.yaml")
		os.Exit(1)
	}

	configPath := flag.Arg(0)
	yamlBytes, err := os.ReadFile(configPath)
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}

	var lines []string
	switch *format {
	case "env":
		lines, err = renderEnvFile(yamlBytes)
	case "shell":
		lines, err = renderShellExports(yamlBytes)
	default:
		fmt.Fprintf(os.Stderr, "Unsupported format: %s\n", *format)
		os.Exit(1)
	}

	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}

	if len(lines) > 0 {
		fmt.Println(strings.Join(lines, "\n"))
	}
}

func renderEnvFile(yamlBytes []byte) ([]string, error) {
	flattened, err := flattenConfig(yamlBytes)
	if err != nil {
		return nil, err
	}

	lines := make([]string, 0, len(flattened))
	for _, key := range sortedKeys(flattened) {
		value := flattened[key]
		if strings.ContainsAny(value, "\r\n") {
			return nil, fmt.Errorf("environment value for %s contains a newline", key)
		}

		lines = append(lines, fmt.Sprintf("%s=%s", key, value))
	}

	return lines, nil
}

func renderShellExports(yamlBytes []byte) ([]string, error) {
	flattened, err := flattenConfig(yamlBytes)
	if err != nil {
		return nil, err
	}

	lines := make([]string, 0, len(flattened))
	for _, key := range sortedKeys(flattened) {
		lines = append(lines, fmt.Sprintf("export %s=%s", key, shellQuote(flattened[key])))
	}

	return lines, nil
}

func flattenConfig(yamlBytes []byte) (map[string]string, error) {
	var root yaml.Node
	if err := yaml.Unmarshal(yamlBytes, &root); err != nil {
		return nil, err
	}

	if len(root.Content) == 0 {
		return map[string]string{}, nil
	}

	document := root.Content[0]
	if document.Kind != yaml.MappingNode {
		return nil, fmt.Errorf("config root must be a YAML mapping")
	}

	output := make(map[string]string)
	if err := flattenNode("", document, output); err != nil {
		return nil, err
	}

	return output, nil
}

func flattenNode(prefix string, node *yaml.Node, output map[string]string) error {
	switch node.Kind {
	case yaml.MappingNode:
		for index := 0; index < len(node.Content); index += 2 {
			keyNode := node.Content[index]
			valueNode := node.Content[index+1]
			if keyNode.Kind != yaml.ScalarNode || keyNode.Value == "" {
				return fmt.Errorf("all config keys must be non-empty strings")
			}

			childPrefix := keyNode.Value
			if prefix != "" {
				childPrefix = prefix + "__" + keyNode.Value
			}

			if err := flattenNode(childPrefix, valueNode, output); err != nil {
				return err
			}
		}
		return nil
	case yaml.SequenceNode:
		for index, child := range node.Content {
			childPrefix := strconv.Itoa(index)
			if prefix != "" {
				childPrefix = prefix + "__" + childPrefix
			}

			if err := flattenNode(childPrefix, child, output); err != nil {
				return err
			}
		}
		return nil
	case yaml.ScalarNode:
		if prefix == "" {
			return fmt.Errorf("scalar values are not allowed at the config root")
		}

		value, err := scalarToString(node)
		if err != nil {
			return err
		}

		output[prefix] = value
		return nil
	default:
		return fmt.Errorf("unsupported YAML node kind: %d", node.Kind)
	}
}

func scalarToString(node *yaml.Node) (string, error) {
	if node.Tag == "!!null" {
		return "", nil
	}

	switch node.Tag {
	case "!!bool":
		if strings.EqualFold(node.Value, "true") {
			return "true", nil
		}
		return "false", nil
	case "!!int", "!!float", "!!str":
		return resolvePlaceholders(node.Value)
	default:
		return resolvePlaceholders(node.Value)
	}
}

func resolvePlaceholders(value string) (string, error) {
	var missing string
	resolved := placeholderPattern.ReplaceAllStringFunc(value, func(match string) string {
		if missing != "" {
			return match
		}

		submatches := placeholderPattern.FindStringSubmatch(match)
		envName := submatches[1]
		envValue, ok := os.LookupEnv(envName)
		if !ok {
			missing = envName
			return match
		}

		return envValue
	})

	if missing != "" {
		return "", fmt.Errorf("required environment variable is not set: %s", missing)
	}

	return resolved, nil
}

func sortedKeys(values map[string]string) []string {
	keys := make([]string, 0, len(values))
	for key := range values {
		keys = append(keys, key)
	}

	sort.Strings(keys)
	return keys
}

func shellQuote(value string) string {
	if value == "" {
		return "''"
	}

	return "'" + strings.ReplaceAll(value, "'", `'"'"'`) + "'"
}
