import { Ref } from 'vue'
import { CombinedError, gql, useQuery } from '@urql/vue'
import { PaginationOffsetQueryVariables } from '../composables/usePaginationOffset'
import { Token } from '../types/generated'
import { ComponentState } from '../composables/useComponentState'

type QueryParams = {
	tokenId: Ref<string>
	contractAddressIndex: Ref<number>
	contractAddressSubIndex: Ref<number>
	eventsVariables: PaginationOffsetQueryVariables
	accountsVariables: PaginationOffsetQueryVariables
}

type TokenQueryResponse = {
	token: Token
}

const TokenQuery = gql`
	query (
		$skipEvent: Int
		$takeEvent: Int
		$skipAccount: Int
		$takeAccount: Int
		$contractAddressIndex: UnsignedLong!
		$contractAddressSubIndex: UnsignedLong!
	) {
		contract(
			contractAddressIndex: $contractAddressIndex
			contractAddressSubIndex: $contractAddressSubIndex
		) {
			transactionHash
			contractAddress
			blockSlotTime
			moduleReference
			amount
			contractName
			creator {
				asString
			}
			contractRejectEvents(skip: $skipRejectEvent, take: $takeRejectEvent) {
				items { ${rejectEventsFragment} }
				totalCount
			}
			contractEvents(skip: $skipEvent, take: $takeEvent) {
				items { ${eventsFragment} }
				totalCount
			}
		}
	}
`

export const useTokenQuery = ({
	tokenId,
	contractAddressIndex,
	contractAddressSubIndex,
	eventsVariables,
	accountsVariables,
}: QueryParams): {
	data: Ref<TokenQueryResponse | undefined>
	error: Ref<CombinedError | undefined>
	componentState: Ref<ComponentState>
	fetching: Ref<boolean>
} => {
	const { data, fetching, error } = useQuery<TokenQueryResponse>({
		query: TokenQuery,
		requestPolicy: 'cache-first',
		variables: {
			tokenId: tokenId.value,
			contractAddressIndex: contractAddressIndex.value,
			contractAddressSubIndex: contractAddressSubIndex.value,
			skipEvent: eventsVariables.skip,
			takeEvent: eventsVariables.take,
			skipAccount: accountsVariables.skip,
			takeAccount: accountsVariables.take,
		},
	})

	const componentState = useComponentState<TokenQueryResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState, fetching }
}
