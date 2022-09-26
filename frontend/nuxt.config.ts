import { defineNuxtConfig } from 'nuxt3'

type Environment = 'dev' | 'stagenet' | 'testnet' | 'mainnet'
type Config = {
	apiUrl: string
	wsUrl: string
}

const ENVIRONMENT = (process.env.ENVIRONMENT as Environment) || 'dev'

const VARS: Record<Environment, Config> = {
	dev: {
		apiUrl: 'https://mainnet.dev-api.ccdscan.io/graphql',
		wsUrl: 'wss://mainnet.dev-api.ccdscan.io/graphql',
	},
	stagenet: {
		apiUrl: 'https://api-ccdscan.stagenet.concordium.com/graphql/',
		wsUrl: 'wss://api-ccdscan.stagenet.concordium.com/graphql',
	},
	testnet: {
		apiUrl: 'https://api-ccdscan.testnet.concordium.com/graphql/',
		wsUrl: 'wss://api-ccdscan.testnet.concordium.com/graphql',
	},
	mainnet: {
		apiUrl: 'https://api-ccdscan.mainnet.concordium.software/graphql/',
		wsUrl: 'wss://api-ccdscan.mainnet.concordium.software/graphql',
	},
}

const getConfig = (env: Environment): Config => {
	return {
		apiUrl: process.env.BACKEND_API_URL || VARS[env].apiUrl || '',
		wsUrl: process.env.BACKEND_WS_URL || VARS[env].wsUrl || '',
	}
}

export default defineNuxtConfig({
	srcDir: 'src/',
	components: [
		'~/components',
		'~/components/atoms',
		'~/components/molecules',
		'~/components/icons',
		'~/components/Table',
		'~/components/Drawer',
		'~/components/BlockDetails',
	],
	publicRuntimeConfig: {
		...getConfig(ENVIRONMENT),
		environment: ENVIRONMENT,
		includeDevTools:
			ENVIRONMENT === 'dev' ||
			ENVIRONMENT === 'stagenet' ||
			ENVIRONMENT === 'testnet',
	},
	nitro: {
		// Workaround: Until whatever depends on Nitro has been upgraded to include 'https://github.com/unjs/nitro/commit/92d711fe936fda0ff877c23d8a0d73ed4ea4adc4', we do it manually.
		// Workaround on workaround: Have to use empty string instead of "node-server" as otherwise we get error "Cannot resolve preset: node-server"
		// (see 'https://forum.cleavr.io/t/cannot-resolve-node-server-preset/686').
		preset: process.env.NITRO_PRESET === 'node-server' ? '' : 'firebase',
	},
	css: ['@/assets/css/styles.css'],
	build: {
		postcss: {
			postcssOptions: require('./postcss.config.cjs'),
		},
	},
})
