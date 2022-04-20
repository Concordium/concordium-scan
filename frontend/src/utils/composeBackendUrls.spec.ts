import { composeBackendUrls } from './composeBackendUrls'

describe('composeBackendUrls', () => {
	const apiUrlProd = 'https://mainnet.api.ccdscan.io/graphql'
	const wsUrlProd = 'wss://mainnet.api.ccdscan.io/graphql'
	const apiUrlStaging = 'https://staging-mainnet.api.ccdscan.io/graphql'
	const wsUrlStaging = 'wss://staging-mainnet.api.ccdscan.io/graphql'

	describe('MAINNET', () => {
		it('should return mainnet URLs if accessing MAINNET site (PRODUCTION)', () => {
			const host = 'ccdscan.io'

			expect(composeBackendUrls(apiUrlProd, wsUrlProd)(host)).toStrictEqual([
				apiUrlProd,
				wsUrlProd,
			])
		})

		it('should return mainnet URLs if accessing MAINNET site (STAGING)', () => {
			const host = 'test.ccdscan.io'

			expect(
				composeBackendUrls(apiUrlStaging, wsUrlStaging)(host)
			).toStrictEqual([apiUrlStaging, wsUrlStaging])
		})
	})

	describe('TESTNET', () => {
		it('should return testnet URLs if accessing TESTNET site (PRODUCTION)', () => {
			const host = 'testnet.ccdscan.io'

			expect(composeBackendUrls(apiUrlProd, wsUrlProd)(host)).toStrictEqual([
				'https://testnet.api.ccdscan.io/graphql',
				'wss://testnet.api.ccdscan.io/graphql',
			])
		})

		it('should return testnet URLs if accessing TESTNET site (STAGING)', () => {
			const host = 'testnet.test.ccdscan.io'

			expect(
				composeBackendUrls(apiUrlStaging, wsUrlStaging)(host)
			).toStrictEqual([
				'https://staging-testnet.api.ccdscan.io/graphql',
				'wss://staging-testnet.api.ccdscan.io/graphql',
			])
		})
	})
})
