import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { NodeStatus } from '~/types/generated'

type NodeDetailResponse = {
	nodeStatus: NodeStatus
}

const NodeDetailQuery = gql<NodeDetailResponse>`
	query ($id: ID!) {
		nodeStatus(id: $id) {
			averageBytesPerSecondIn
			averageBytesPerSecondOut
			averagePing
			bakingCommitteeMember
			bestArrivedTime
			bestBlock
			bestBlockBakerId
			bestBlockCentralBankAmount
			bestBlockExecutionCost
			bestBlockHeight
			bestBlockTotalAmount
			bestBlockTotalEncryptedAmount
			bestBlockTransactionCount
			bestBlockTransactionEnergyCost
			bestBlockTransactionsSize
			blockArriveLatencyEma
			blockArriveLatencyEmsd
			blockArrivePeriodEma
			blockArrivePeriodEmsd
			blockReceiveLatencyEma
			blockReceiveLatencyEmsd
			blockReceivePeriodEma
			blockReceivePeriodEmsd
			blocksReceivedCount
			blocksVerifiedCount
			clientVersion
			consensusBakerId
			consensusRunning
			finalizationCommitteeMember
			finalizationCount
			finalizationPeriodEma
			finalizationPeriodEmsd
			finalizedBlock
			finalizedBlockHeight
			finalizedBlockParent
			finalizedTime
			genesisBlock
			id
			nodeId
			nodeName
			packetsReceived
			packetsSent
			peersCount
			peersList {
				nodeStatus {
					nodeId
					nodeName
					id
				}
				nodeId
				__typename
			}
			peerType
			transactionsPerBlockEma
			transactionsPerBlockEmsd
			uptime
		}
	}
`

export const useNodeDetailQuery = (id: NodeStatus['id']) => {
	const { data, fetching, error } = useQuery({
		query: NodeDetailQuery,
		requestPolicy: 'cache-and-network',
		variables: { id },
	})

	const componentState = useComponentState<NodeDetailResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState }
}
