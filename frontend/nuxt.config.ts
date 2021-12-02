import { defineNuxtConfig } from 'nuxt3'

export default defineNuxtConfig({
	nitro: {
		preset: 'server',
	},
	css: ['@/assets/css/styles.css'],
	build: {
		postcss: {
			postcssOptions: require('./postcss.config.cjs'),
		},
	},
})
