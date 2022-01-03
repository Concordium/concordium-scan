export type Block = {
	id: number
	bakerId?: number
	blockHash: string
	blockHeight: number
	blockSlotTime: string
	finalized: boolean
	transactionCount: number
}

export type BlockList = {
	blocks: {
		nodes: Block[]
	}
}
