module.exports = {
	preset: 'ts-jest',
	testEnvironment: 'jsdom',
	moduleFileExtensions: ['ts', 'js', 'json', 'vue'],
	transform: {
		'^.+.vue$': 'vue-jest',
		'^.+.jsx?$': '<rootDir>/node_modules/babel-jest',
		'^.+.js$': '<rootDir>/node_modules/babel-jest',
	},
	setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
	testMatch: ['<rootDir>/**/*.spec.ts'],
	verbose: true,
}
