import { defineNuxtConfig } from 'nuxt/config'

export default defineNuxtConfig({
	// Configuration available to the application at runtime, note the `public` object will be
	// expose directly in the client-side code and therefore should not contain secrets.
	// Below values are default and can be overwritten by environment variables at runtime.
	runtimeConfig: {
		public: {
			version: process.env.npm_package_version,
			// URL to use when sending GraphQL queries to the CCDscan API.
			// (env NUXT_PUBLIC_API_URL)
			apiUrl: process.env.NUXT_PUBLIC_API_URL,
			// URL to use when using websockets in GraphQL CCDscan API.
			// (env NUXT_PUBLIC_WS_URL)
			wsUrl: process.env.NUXT_PUBLIC_WS_URL,
			// URL to use when sending GraphQL queries to the CCDscan API.
			// (env NUXT_PUBLIC_API_URL_RUST)
			apiUrlRust: process.env.NUXT_PUBLIC_API_URL_RUST,
			// URL to use when using websockets in GraphQL CCDscan API.
			// (env NUXT_PUBLIC_WS_URL_RUST)
			wsUrlRust: process.env.NUXT_PUBLIC_WS_URL_RUST,
			// Settings for how to display the explorer.
			explorer: {
				// The name to display for the explorer.
				// (env NUXT_PUBLIC_EXPLORER_NAME)
				name: process.env.NUXT_PUBLIC_EXPLORER_NAME,
				// The list of external explorers to link in the explorer selector.
				// Should be provided as `<name>@<url>` separated by `;`.
				// Ex.: 'Mainnet@https://ccdscan.io;Testnet@https://testnet.ccdscan.io'
				// (env NUXT_PUBLIC_EXPLORER_EXTERNAL).
				external: process.env.NUXT_PUBLIC_EXPLORER_EXTERNAL,
			},
			// When enabled a hint for the current breakpoint (related to screen size)
			// is displayed in the bottom left corner. Enable only for development.
			// (env NUXT_PUBLIC_ENABLE_BREAKPOINT_HINT)
			enableBreakpointHint: process.env.NUXT_PUBLIC_ENABLE_BREAKPOINT_HINT,
			// Enabled the urql-devtools for debugging GraphQL (require browser extension).
			// Enable only for development.
			// (env NUXT_PUBLIC_ENABLE_URQL_DEVTOOLS)
			enableUrqlDevtools: process.env.NUXT_PUBLIC_ENABLE_URQL_DEVTOOLS,

			//Environment config for stable coin menu display
			enablePltFeatures: process.env.NUXT_PUBLIC_ENABLE_PLT_FEATURES,
		},
	},
	// Directory for finding the source files.

	srcDir: 'src/',
	// Directories to search for components.
	components: [
		'~/components',
		'~/components/atoms',
		'~/components/molecules',
		'~/components/icons',
		'~/components/Table',
		'~/components/Drawer',
		'~/components/BlockDetails',
	],
	// Global CSS files
	css: ['~/assets/css/styles.css'],
	// Enable postCSS
	postcss: {
		plugins: {
			tailwindcss: {},
			autoprefixer: {},
		},
	},
	// Lock default values for Nuxt to what they were at this date.
	compatibilityDate: '2024-11-01',
	// TypeScript configurations.
	typescript: {
		// Enable strict checks.
		strict: true,
		// Enable type-checking at build time.
		typeCheck: true,
	},
	modules: ['@nuxt/eslint'],
})
