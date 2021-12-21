type Block = {
	status: 'Pending' | 'Finalized'
	timestamp: string
	hash: string
	transactions: number
	baker: string
	reward: number
}

export const blocks: Block[] = [
	{
		status: 'Pending',
		timestamp: 'Just now',
		hash: 'dec34a',
		transactions: 45,
		baker: '0c2c31',
		reward: 0.006423,
	},
	{
		status: 'Finalized',
		timestamp: 'Less than a minute ago',
		hash: 'dfs271',
		transactions: 0,
		baker: 'eb0c31',
		reward: 0.006438,
	},
	{
		status: 'Finalized',
		timestamp: 'Less than a minute ago',
		hash: '34ced2',
		transactions: 28,
		baker: 'eb0c31',
		reward: 0.006499,
	},
	{
		status: 'Finalized',
		timestamp: '1 minute ago',
		hash: '0c2c31',
		transactions: 3,
		baker: 'eb0c31',
		reward: 0.006582,
	},
]
