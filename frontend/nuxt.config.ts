// There seems to be some dependency errors in Nuxt3
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-nocheck
import { defineNuxtConfig } from 'nuxt3'

export default defineNuxtConfig({
	components: [
		'~/components',
		'~/components/atoms',
		'~/components/icons',
		'~/components/Table',
	],
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
