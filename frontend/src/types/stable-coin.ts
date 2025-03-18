export type StableCoin = {
	name: string
	symbol: string
	decimal: number
	contract_address: string
	totalSupply: string
	circulating_supply: string
	transfers?: Array<Transfers> | null | undefined
}

export type Transfers = {
	from: string
	to: string
	asset_name: string
	date: string
	amount: string
}
