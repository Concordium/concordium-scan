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
		...VARS[ENVIRONMENT],
		environment: ENVIRONMENT,
		includeDevTools: ENVIRONMENT === 'dev' || ENVIRONMENT === 'stagenet' || ENVIRONMENT === 'testnet',
	},
	nitro: {
		preset: 'firebase',
	},
	css: ['@/assets/css/styles.css'],
	build: {
		postcss: {
			postcssOptions: require('./postcss.config.cjs'),
		},
	},
})
