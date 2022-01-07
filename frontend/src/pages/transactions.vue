<template>
	<div>
		<Title>CCDScan | Transactions</Title>
		<main class="p-4">
			<Table>
				<TableHead>
					<TableRow>
						<TableTh>Status</TableTh>
						<TableTh>Type</TableTh>
						<TableTh>Transaction hash</TableTh>
						<TableTh>Sender</TableTh>
						<TableTh align="right">Cost (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow
						v-for="transaction in data?.transactions.nodes"
						:key="transaction.transactionHash"
					>
						<TableTd>
							<StatusCircle
								:class="[
									'h-4 mr-2 text-theme-interactive',
									{ 'text-theme-error': !transaction.result.successful },
								]"
							/>
							{{ transaction.result.successful ? 'Finalised' : 'Rejected' }}
						</TableTd>
						<TableTd>{{
							translateTransactionType(transaction.transactionType)
						}}</TableTd>
						<TableTd :class="$style.numerical">
							<HashtagIcon :class="$style.cellIcon" />
							{{ transaction.transactionHash.substring(0, 6) }}
						</TableTd>
						<TableTd :class="$style.numerical">
							<UserIcon
								v-if="transaction.senderAccountAddress"
								:class="$style.cellIcon"
							/>
							{{ transaction.senderAccountAddress?.substring(0, 6) }}
						</TableTd>
						<TableTd align="right" :class="$style.numerical">
							{{ convertMicroCcdToCcd(transaction.ccdCost) }}
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>
		</main>
	</div>
</template>

<script lang="ts" setup>
import { useQuery, gql } from '@urql/vue'
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid'
import { convertMicroCcdToCcd } from '~/utils/format'
import {
	translateTransactionType,
	type TransactionType,
} from '~/utils/translateTransactionTypes'

// Splitting the types out will cause an import error, as they are are not
// bundled by Nuxt. See more in README.md under "Known issues"
type Transaction = {
	__typename: string
	transactionHash: string
	senderAccountAddress: string
	ccdCost: number
	result: {
		successful: boolean
	}
	transactionType: TransactionType
}

type TransactionList = {
	transactions: {
		nodes: Transaction[]
	}
}

const TransactionsQuery = gql<TransactionList>`
	query {
		transactions {
			nodes {
				transactionHash
				senderAccountAddress
				ccdCost
				energyCost
				__typename
				result {
					successful
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
		}
	}
`

const { data } = useQuery({
	query: TransactionsQuery,
	requestPolicy: 'cache-and-network',
})
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-theme-interactive;
}
.cellIcon {
	@apply h-4 text-theme-white inline align-baseline;
}

.numerical {
	@apply font-mono;
	font-variant-ligatures: none;
}
</style>
