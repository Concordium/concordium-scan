/**
 * Curried function to compose backend URLs for mainnet/testnet based on the location URL
 * Returns a tuple with the composed apiUrl and wsUrl
 * @param apiUrl string - API URL coming from config
 * @param wsUrl string - WebSocket URL coming from config
 * @param host string - (in returned fn) current location.host
 * @returns {Array} - A tuple with [apiUrl, wsUrl]
 * @example
 * const [composedApiUrl, composedWsUrl] = composeBackendUrls(apiUrl, wsUrl)(location.host)
 */
export const composeBackendUrls =
	(apiUrl: string, wsUrl: string) => (host: Location['host']) =>
		host.includes('testnet')
			? [
					apiUrl.replace('mainnet', 'testnet'),
					wsUrl.replace('mainnet', 'testnet'),
			  ]
			: [apiUrl, wsUrl]
