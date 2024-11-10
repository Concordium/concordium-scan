// @ts-check
import withNuxt from './.nuxt/eslint.config.mjs'
// @ts-expect-error No type definitions for this one.
import eslintConfigPrettier from 'eslint-config-prettier'

export default withNuxt([
	// Ignore files generated from the GraphQL schema, and Nuxt output directories.
	{ ignores: ['src/types/generated.ts', 'dist', '.output'] },
	// Disable rules conflicting with prettier.
	eslintConfigPrettier,
])
	.override('nuxt/vue/rules', {
		rules: {
			'vue/multi-word-component-names': 'off',
			// Disable check, since this is mostly relevant for older version of Vue (version 2).
			'vue/no-multiple-template-root': 'off',
		},
	})
	// Reduce certain rules to be warnings instead of errors, since these might
	// appear during development and are still prevented by the CI.
	.override('nuxt/typescript/rules', {
		rules: {
			'@typescript-eslint/no-unused-vars': 'warn',
			'@typescript-eslint/no-unused-expressions': 'warn',
			'@typescript-eslint/consistent-type-imports': 'warn',
			'@typescript-eslint/no-import-type-side-effects': 'warn',
		},
	})
	.override('nuxt/rules', { rules: { 'no-empty': 'warn' } })
