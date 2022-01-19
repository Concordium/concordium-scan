<template>
	<div>
		<Title>CCDScan | Transactions</Title>
		<main class="p-4">
			<Table>
				<TableHead>
					<TableRow>
						<TableTh width="14.3%">Status</TableTh>
						<TableTh width="28.5%">Type</TableTh>
						<TableTh width="14.3%">Transaction hash</TableTh>
						<TableTh width="14.3%">Block height</TableTh>
						<TableTh width="14.3%">Sender</TableTh>
						<TableTh width="14.3%" align="right">Cost (Ͼ)</TableTh>
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
								@click="selectedTxId = transaction.id"
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

			<Pagination
				v-if="data?.transactions.pageInfo"
				:page-info="data?.transactions.pageInfo"
				:go-to-page="goToPage"
			/>
		</main>
	</div>
</template>

<script lang="ts" setup>
import { useQuery, gql } from '@urql/vue'
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid/index.js'
import { convertMicroCcdToCcd } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import type { Transaction } from '~/types/transactions'
import type { PageInfo } from '~/types/pageInfo'
import { usePagination } from '~/composables/usePagination'

const { afterCursor, beforeCursor, paginateFirst, paginateLast, goToPage } =
	usePagination()

const selectedTxId = useTransactionDetails()

type TransactionsResponse = {
	transactions: {
		nodes: Transaction[]
		pageInfo: PageInfo
	}
}

const TransactionsQuery = gql<TransactionsResponse>`
	query ($after: String, $before: String, $first: Int, $last: Int) {
		transactions(after: $after, before: $before, first: $first, last: $last) {
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
			pageInfo {
				startCursor
				endCursor
				hasPreviousPage
				hasNextPage
			}
		}
	}
`

const { data } = useQuery({
	query: TransactionsQuery,
	requestPolicy: 'cache-and-network',
	variables: {
		after: afterCursor,
		before: beforeCursor,
		first: paginateFirst,
		last: paginateLast,
	},
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
