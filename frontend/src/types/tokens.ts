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
	artifacts?: UrlJson
	assets?: TokenMetadata[]
	attributes?: TokenAttribute[]
	localization?: { [name: string]: UrlJson }
}
