module.exports = {
	preset: 'ts-jest',
	testEnvironment: 'jsdom',
	roots: ['<rootDir>/src/'],
	moduleFileExtensions: ['ts', 'js', 'json', 'vue'],
	moduleNameMapper: {
		'^~/(.*)$': '<rootDir>/src/$1',
		'^#app': '<rootDir>/node_modules/nuxt3/dist/app/index.d.ts',
	},
	transform: {
		'^.+.vue$': '@vue/vue3-jest',
		'^.+.jsx?$': '<rootDir>/node_modules/babel-jest',
		'^.+.js$': '<rootDir>/node_modules/babel-jest',
	},
	transformIgnorePatterns: ['node_modules/(?!nuxt3)/'],
	setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
	testMatch: ['<rootDir>/src/**/*.spec.ts'],
	verbose: true,
}
