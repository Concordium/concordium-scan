<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Hash</TableTh>
					<TableTh>Restake earnings?</TableTh>
					<TableTh class="text-right">Amount (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="delegator in delegators"
					:key="delegator.accountAddress.asString"
				>
					<TableTd>
						<AccountLink :address="delegator.accountAddress.asString" />
					</TableTd>
					<TableTd>
						<div class="whitespace-normal">
							<span v-if="delegator.restakeEarnings">Yes</span>
							<span v-else>No</span>
						</div>
					</TableTd>
					<TableTd class="text-right">
						<Amount :amount="delegator.stakedAmount" :show-symbol="true" />
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination
			v-if="pageInfo && (pageInfo.hasNextPage || pageInfo.hasPreviousPage)"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import type { PaginationTarget } from '~/composables/usePagination'
import type { PageInfo, PassiveDelegationSummary } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	delegators: PassiveDelegationSummary[]
	pageInfo: PageInfo
	totalCount: number
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
