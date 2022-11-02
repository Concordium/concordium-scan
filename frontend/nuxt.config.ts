import { defineNuxtConfig } from 'nuxt3'

type Environment = 'dev' | 'stagenet' | 'testnet' | 'mainnet'
type Config = {
	apiUrl: string
	wsUrl: string
}

const ENVIRONMENT = (process.env.ENVIRONMENT as Environment) || 'dev'

const VARS: Record<Environment, Config> = {
	dev: {
		apiUrl: 'http://localhost:5000/graphql',
		wsUrl: 'ws://localhost:5000/graphql',
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
		preset: 'firebase',
	},
	css: ['@/assets/css/styles.css'],
	build: {
		postcss: {
			postcssOptions: require('./postcss.config.cjs'),
		},
	},
})
