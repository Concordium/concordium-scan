import { defineNuxtConfig } from 'nuxt3'

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
		apiUrl: 'https://mainnet.dev-api.ccdscan.io/graphql',
		wsUrl: 'wss://mainnet.dev-api.ccdscan.io/graphql',
		includeDevTools: true,
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
