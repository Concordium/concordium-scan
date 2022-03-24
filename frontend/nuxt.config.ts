import { defineNuxtConfig } from 'nuxt3'

type Environment = 'dev' | 'test' | 'prod'
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
	test: {
		apiUrl: 'https://staging-mainnet.api.ccdscan.io/graphql/',
		wsUrl: 'wss://staging-mainnet.api.ccdscan.io/graphql',
	},
	prod: {
		apiUrl: 'https://mainnet.api.ccdscan.io/graphql/',
		wsUrl: 'wss://mainnet.api.ccdscan.io/graphql',
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
		includeDevTools: ENVIRONMENT === 'dev' || ENVIRONMENT === 'test',
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
