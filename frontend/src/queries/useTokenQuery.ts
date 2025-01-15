import type { Ref } from 'vue'
import { type CombinedError, gql, useQuery } from '@urql/vue'
import type { PaginationOffsetQueryVariables } from '../composables/usePaginationOffset'
import type { Token } from '../types/generated'
import type { ComponentState } from '../composables/useComponentState'

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

const tokenEventsFragment = `
__typename
tokenId
contractIndex
contractSubIndex
transaction {
  transactionHash
}
event {
  __typename
  ...on Cis2BurnEvent {
    fromAddress {
      __typename
      ... on AccountAddress {
        asString
      }
      ... on ContractAddress {
        index
        subIndex
        asString
      }
    }
    tokenAmount
    tokenId
  }
  ...on Cis2MintEvent {
    toAddress {
      __typename
      ... on AccountAddress {
        asString
      }
      ... on ContractAddress {
        index
        subIndex
        asString
      }
    }
    tokenAmount
    tokenId
  }
  ...on Cis2TokenMetadataEvent {
    hashHex
    metadataUrl
    tokenId
  }
  ... on Cis2TransferEvent {
    toAddress {
      __typename
      ... on AccountAddress {
        asString
      }
      ... on ContractAddress {
        index
        subIndex
        asString
      }
    }
    fromAddress {
      __typename
      ... on AccountAddress {
        asString
      }
      ... on ContractAddress {
        index
        subIndex
        asString
      }
    }
    tokenAmount
    tokenId
  }
}
`

const TokenQuery = gql`
query (
	$skipEvent: Int
	$takeEvent: Int
	$skipAccount: Int
	$takeAccount: Int
  $tokenId: String!
	$contractAddressIndex: UnsignedLong!
	$contractAddressSubIndex: UnsignedLong!
) {
	token(
    tokenId: $tokenId
		contractIndex: $contractAddressIndex
		contractSubIndex: $contractAddressSubIndex
	) {
    tokenId
    contractIndex
    contractSubIndex
    metadataUrl
    totalSupply
    tokenAddress
    contractAddressFormatted
    initialTransaction {
      block {
        blockSlotTime
      }
    }    
    accounts(skip: $skipAccount, take: $takeAccount) {
      items {
        accountId
        account {
          address {
            asString
          }
        }        
        balance
        contractIndex
        contractSubIndex
        tokenId
      }
      totalCount
    }
    tokenEvents(skip: $skipEvent, take: $takeEvent) {
      items { ${tokenEventsFragment} }
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
