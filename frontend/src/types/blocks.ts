import type { PageInfo, Transaction } from './generated'

/** @deprecated Use generated type "Mint" instead */
export type Mint = {
	bakingReward: number
	finalizationReward: number
	foundationAccount: number
	platformDevelopmentCharge: number
}

/** @deprecated Use generated type "FinalizationReward" instead */
export type FinalizationReward = {
	address: string
	amount: number
}

/** @deprecated Use generated type "FinalizationRewards" instead */
export type FinalizationRewards = {
	remainder: number
	rewards: {
		nodes: FinalizationReward[]
		pageInfo: PageInfo
	}
}

/** @deprecated Use generated type "BlockRewards" instead */
export type BlockRewards = {
	bakerAccountAddress: string
	bakerReward: number
	foundationAccountAddress: string
	foundationCharge: number
	newGasAccount: number
	oldGasAccount: number
	transactionFees: number
}

/** @deprecated Use generated type "SpecialEvents" instead */
type SpecialEvents = {
	mint?: Mint
	blockRewards?: BlockRewards
	finalizationRewards?: FinalizationRewards
}

/** @deprecated Use generated type "Block" instead */
export type Block = {
	id: string
	bakerId?: number
	blockHash: string
	blockHeight: number
	blockSlotTime: string
	finalized: boolean
	transactionCount: number
	transactions: {
		nodes: Transaction[]
		pageInfo: PageInfo
	}
	specialEvents: SpecialEvents
}

/** @deprecated Use generated type "Subscription" instead */
export type BlockSubscriptionResponse = {
	blockAdded: Block
}
