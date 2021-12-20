import { defineNuxtConfig } from 'nuxt3'

export default defineNuxtConfig({
	components: [
		'~/components',
		'~/components/atoms',
		'~/components/icons',
		'~/components/Table',
	],
	publicRuntimeConfig: {
		apiUrl:
			'http://ftbccscandevnode.northeurope.cloudapp.azure.com:5000/graphql/',
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
