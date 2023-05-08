export async function fetchMetadata(metadataUrl?: string) {
	if (!metadataUrl) return Promise.reject(new Error('No metadata URL provided'))
	if (typeof metadataUrl !== 'string')
		return Promise.reject(new Error('Metadata URL is not a string'))
	if (!metadataUrl?.startsWith('http'))
		return Promise.reject(new Error('Metadata URL is not a valid URL'))

	try {
		const url = new URL(String(metadataUrl))
		const response = await fetch(url, {
			cache: 'force-cache',
			redirect: 'error',
			keepalive: false,
			method: 'GET',
			// headers: new Headers({
			// 	'Content-Type': 'application/json',
			// }),
		})

		return response.json()
	} catch {}
}
