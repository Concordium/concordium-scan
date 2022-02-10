<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Status</TableTh>
					<TableTh>Transaction hash</TableTh>
					<TableTh>Sender</TableTh>
					<TableTh align="right">Cost (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="transaction in transactions"
					:key="transaction.transactionHash"
				>
					<TableTd>
						<StatusCircle
							:class="[
								'h-4 mr-2 text-theme-interactive',
								{
									'text-theme-error':
										transaction.result.__typename === 'Rejected',
								},
							]"
						/>
						{{
							transaction.result.__typename === 'Success'
								? 'Success'
								: 'Rejected'
						}}
					</TableTd>
					<TableTd class="numerical">
						<HashtagIcon class="h-4 text-theme-white inline align-baseline" />
						<LinkButton
							class="numerical"
							@click="
								drawer.push(
									'transaction',
									transaction.transactionHash,
									transaction.id
								)
							"
						>
							<Tooltip
								:text="transaction.transactionHash"
								text-class="text-theme-body"
							>
								{{ shortenHash(transaction.transactionHash) }}
							</Tooltip>
						</LinkButton>
					</TableTd>
					<TableTd class="numerical">
						<UserIcon
							v-if="transaction.senderAccountAddress"
							class="h-4 text-theme-white inline align-baseline"
						/>
						<Tooltip
							:text="transaction.senderAccountAddress"
							text-class="text-theme-body"
						>
							{{ shortenHash(transaction.transactionHash) }}
						</Tooltip>
					</TableTd>
					<TableTd align="right" class="numerical">
						{{ convertMicroCcdToCcd(transaction.ccdCost) }}
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination
			v-if="pageInfo && totalCount > PAGE_SIZE"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import { UserIcon, HashtagIcon } from '@heroicons/vue/solid/index.js'
import { convertMicroCcdToCcd, shortenHash } from '~/utils/format'
import { PAGE_SIZE } from '~/composables/usePagination'
import type { PaginationTarget } from '~/composables/usePagination'
import type { Transaction } from '~/types/transactions'
import type { PageInfo } from '~/types/generated'
import { useDrawer } from '~/composables/useDrawer'

type Props = {
	transactions: Transaction[]
	totalCount: number
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
const drawer = useDrawer()
defineProps<Props>()
</script>
