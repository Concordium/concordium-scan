/**
 * @file
 * Interfaces specified at https://proposals.concordium.software/CIS/cis-2.html#abstract
 */

import { Token } from './generated'

interface UrlJson {
	url: string
	hash?: string
}

interface TokenAttribute {
	type: string
	name: string
	value: string
}

export interface TokenMetadata {
	name?: string
	symbol?: string
	unique?: boolean
	decimals?: number
	description?: string
	thumbnail?: UrlJson
	display?: UrlJson
	artifact?: UrlJson
	assets?: TokenMetadata[]
	attributes?: TokenAttribute[]
	localization?: { [name: string]: UrlJson }
}

export interface TokenWithMetadata extends Token {
	metadata?: TokenMetadata
}

export async function fetchMetadata(
	metadataUrl?: string
): Promise<TokenMetadata | undefined> {
	if (!metadataUrl) return Promise.reject(new Error('No metadata URL provided'))
	if (typeof metadataUrl !== 'string')
		return Promise.reject(new Error('Metatadata URL is not a string'))
	if (!metadataUrl.startsWith('http'))
		return Promise.reject(new Error('Metadata URL is not a valid URL'))
	try {
		const url = new URL(metadataUrl)
		const response = await fetch(url, {
			cache: 'force-cache',
			redirect: 'error',
			keepalive: false,
		})
		return response.json() as TokenMetadata
	} catch {}
}
