module.exports = {
	preset: 'ts-jest',
	testEnvironment: 'jsdom',
	roots: ['<rootDir>/src/'],
	moduleFileExtensions: ['ts', 'js', 'json', 'vue'],
	moduleNameMapper: {
		'^~/(.*)$': '<rootDir>/src/$1',
	},
	transform: {
		'^.+.vue$': 'vue-jest',
		'^.+.jsx?$': '<rootDir>/node_modules/babel-jest',
		'^.+.js$': '<rootDir>/node_modules/babel-jest',
	},
	setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
	testMatch: ['<rootDir>/src/**/*.spec.ts'],
	verbose: true,
}
