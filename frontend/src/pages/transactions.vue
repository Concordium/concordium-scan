<template>
	<div>
		<Title>CCDScan | Transactions</Title>
		<TransactionDetails
			:transaction-id="selectedTxId"
			:on-close="closeDrawer"
		/>
		<main class="p-4">
			<Table>
				<TableHead>
					<TableRow>
						<TableTh>Status</TableTh>
						<TableTh>Type</TableTh>
						<TableTh>Transaction hash</TableTh>
						<TableTh>Block height</TableTh>
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
							{{ transaction.result.successful ? 'Success' : 'Rejected' }}
						</TableTd>
						<TableTd>{{
							translateTransactionType(transaction.transactionType)
						}}</TableTd>
						<TableTd>
							<HashtagIcon :class="$style.cellIcon" />
							<LinkButton
								:class="$style.numerical"
								@click="openDrawer(transaction.id)"
							>
								{{ transaction.transactionHash.substring(0, 6) }}
							</LinkButton>
						</TableTd>
						<TableTd :class="$style.numerical">{{
							transaction.blockHeight
						}}</TableTd>
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
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import type { Transaction } from '~/types/transactions'

const selectedTxId = ref('')

const openDrawer = (id: string) => {
	selectedTxId.value = id
}

const closeDrawer = () => {
	selectedTxId.value = ''
}

type TransactionsResponse = {
	transactions: {
		nodes: Transaction[]
	}
}

const TransactionsQuery = gql<TransactionsResponse>`
	query {
		transactions {
			nodes {
				id
				ccdCost
				blockHeight
				transactionHash
				senderAccountAddress
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
