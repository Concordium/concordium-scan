import { fetchMetadata } from './tokenUtils'

describe('tokenUtils', () => {
	// Tests for fetchMetadata
	describe('fetchMetadata', () => {
		const metadataActual = { test: 100 }

		beforeEach(() => {
			global.fetch = jest.fn(() =>
				Promise.resolve({
					json: () => Promise.resolve(metadataActual),
				})
			) as jest.Mock
		})

		it('should return a promise', async () => {
			const metadata = await fetchMetadata('https://example.com')
			expect(metadata).toEqual(metadataActual)
		})

		it('should throw an error if no metadata URL is provided', async () => {
			await expect(fetchMetadata()).rejects.toThrow('No metadata URL provided')
		})

		it('should throw an error if metadata URL is not a string', async () => {
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			await expect(fetchMetadata(100 as any)).rejects.toThrow(
				'Metadata URL is not a string'
			)
		})

		it('should throw an error if metadata URL is not a valid URL', async () => {
			await expect(fetchMetadata('example.com')).rejects.toThrow(
				'Metadata URL is not a valid URL'
			)
		})
	})
})
