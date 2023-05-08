import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import { useComponentState } from '~/composables/useComponentState'
import type { Account, Scalars, Token } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type TokenQueryVariables = {
	contractIndex: Ref<Scalars['UnsignedLong']>
	contractSubIndex: Ref<Scalars['UnsignedLong']>
	tokenId: Ref<Scalars['String']>
	accountFirst: QueryVariables['first']
	accountLast: QueryVariables['last']
	accountBefore: QueryVariables['before']
	accountAfter: QueryVariables['after']
	txnFirst: QueryVariables['first']
	txnLast: QueryVariables['last']
	txnAfter: QueryVariables['after']
	txnBefore: QueryVariables['before']
}

type TokenQueryResponse = {
	token: Token
}

const AddressQueryFragment = `
	{
		__typename
		... on AccountAddress {
			asString
		}
		... on ContractAddress {
			index
			subIndex
		}
	}
`

const query = gql`
	query (
		$contractIndex: UnsignedLong!
		$contractSubIndex: UnsignedLong!
		$tokenId: String!
		$accountFirst: Int
		$accountLast: Int
		$accountAfter: String
		$accountBefore: String
		$txnFirst: Int
		$txnLast: Int
		$txnAfter: String
		$txnBefore: String
		) {
		token(contractIndex: $contractIndex, contractSubIndex: $contractSubIndex, tokenId: $tokenId) {
			contractIndex
			contractSubIndex
			tokenId
			metadataUrl
			totalSupply
			accounts(first: $accountFirst, last: $accountLast, before: $accountBefore, after: $accountAfter) {
				nodes {
					balance
					account {
						address {
							asString
						}
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			createTransaction {
				block {
					blockSlotTime
				}
			}
			transactions(first: $txnFirst, last: $txnLast, before: $txnBefore, after: $txnAfter) {
				nodes {
					transaction {
						id
						transactionHash
						senderAccountAddress {
							asString
						}
						ccdCost
						block {
							blockSlotTime
						}
						result {
							__typename

						}
						transactionType {
						__typename
							... on AccountTransaction {
								accountTransactionType
							}
							... on CredentialDeploymentTransaction {
								credentialDeploymentTransactionType
							}
							... on UpdateTransaction {
								updateTransactionType
							}
						}
					}
					data {
						__typename
						...on CisEventDataBurn {
							amount
							from ${AddressQueryFragment}
						}
						...on CisEventDataMetadataUpdate {
							metadataUrl
							metadataHashHex
						}
						...on CisEventDataMint {
							amount
							to ${AddressQueryFragment}
						}
						...on CisEventDataTransfer {
							amount
							from ${AddressQueryFragment}
							to ${AddressQueryFragment}
						}
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
		}
	}
`

export const useTokenQuery = (variables: TokenQueryVariables) => {
	const { data, fetching, error, executeQuery } = useQuery<
		TokenQueryResponse,
		TokenQueryVariables
	>({
		query,
		variables,
		requestPolicy: 'network-only',
	})

	const componentState = useComponentState<TokenQueryResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState, executeQuery }
}
