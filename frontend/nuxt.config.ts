import { defineNuxtConfig } from 'nuxt3'

export default defineNuxtConfig({
	srcDir: 'src/',
	components: [
		'~/components',
		'~/components/atoms',
		'~/components/icons',
		'~/components/Table',
		'~/components/Drawer',
		'~/components/BlockDetails',
	],
	publicRuntimeConfig: {
		apiUrl: 'https://dev.api-mainnet.ccdscan.io/graphql/',
		wsUrl: 'wss://dev.api-mainnet.ccdscan.io/graphql',
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
