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

// types.ts
export enum TransactionFilterOption {
	Top20 = 20,
	Top50 = 50,
	Top100 = 100,
}

// Optional: If you want labels for UI
export const transactionFilterOptions = [
	{ label: 'Top 20', value: TransactionFilterOption.Top20 },
	{ label: 'Top 50', value: TransactionFilterOption.Top50 },
	{ label: 'Top 100', value: TransactionFilterOption.Top100 },
]
export const holderFilterOptions = [
	{ label: 'Top 20', value: TransactionFilterOption.Top20 },
	{ label: 'Top 50', value: TransactionFilterOption.Top50 },
	{ label: 'Top 100', value: TransactionFilterOption.Top100 },
]
// types.ts
// Enum for metrics periods
export enum TopHolderFilterOption {
	Last7Days = 7,
	Last30Days = 30,
	Last90Days = 90,
}

// UI-friendly label/value mapping
export const topHolderFilterOptions = [
	{ label: '7 Days', value: TopHolderFilterOption.Last7Days },
	{ label: '1 Month', value: TopHolderFilterOption.Last30Days },
	{ label: '3 Months', value: TopHolderFilterOption.Last90Days },
]
