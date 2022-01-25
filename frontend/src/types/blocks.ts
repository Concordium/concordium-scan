import type { Transaction } from './transactions'
import type { PageInfo } from './pageInfo'

export type Mint = {
	bakingReward: number
	finalizationReward: number
	foundationAccount: number
	platformDevelopmentCharge: number
}

export type FinalizationReward = {
	address: string
	amount: number
}

export type FinalizationRewards = {
	remainder: number
	rewards: {
		nodes: FinalizationReward[]
		pageInfo: PageInfo
	}
}

export type BlockRewards = {
	bakerAccountAddress: string
	bakerReward: number
	foundationAccountAddress: string
	foundationCharge: number
	newGasAccount: number
	oldGasAccount: number
	transactionFees: number
}

type SpecialEvents = {
	mint?: Mint
	blockRewards?: BlockRewards
}

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
	}
	specialEvents: SpecialEvents
}

export type BlockSubscriptionResponse = {
	blockAdded: Block
}
