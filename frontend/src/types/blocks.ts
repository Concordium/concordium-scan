import type { Transaction } from './transactions'

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
}
