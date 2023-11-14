<template>
	<TokenomicsDisplay class="p-4 pr-0">
		<template #title>Block rewards</template>
		<template #content>
			<Table v-for="bakingRewards in data.nodes" :key="bakingRewards.id">
				<TableHead>
					<TableRow>
						<TableTh>Validator</TableTh>
						<TableTh align="right">Reward (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow
						v-for="baker in bakingRewards.bakingRewards?.nodes"
						:key="baker.accountAddress.asString"
					>
						<TableTd>
							<AccountLink :address="baker.accountAddress.asString" />
						</TableTd>
						<TableTd align="right" class="numerical">
							<Amount :amount="baker.amount" />
						</TableTd>
					</TableRow>
				</TableBody>
				<TableFooter>
					<TableRow>
						<TableTd colspan="2" variant="with-background">
							<Pagination
								v-if="
									bakingRewards.bakingRewards?.pageInfo &&
									(bakingRewards.bakingRewards.pageInfo.hasNextPage ||
										bakingRewards.bakingRewards.pageInfo.hasPreviousPage)
								"
								position="relative"
								size="sm"
								:page-info="bakingRewards.bakingRewards.pageInfo"
								:go-to-page="goToSubPage"
							/>
						</TableTd>
					</TableRow>
				</TableFooter>
			</Table>
			<Pagination
				v-if="data.pageInfo.hasNextPage || data.pageInfo.hasPreviousPage"
				class="relative"
				:page-info="data.pageInfo"
				:go-to-page="goToPage"
			/>
		</template>
	</TokenomicsDisplay>
</template>

<script lang="ts" setup>
import TokenomicsDisplay from './TokenomicsDisplay.vue'
import Amount from '~/components/atoms/Amount.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import Table from '~/components/Table/Table.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableTh from '~/components/Table/TableTh.vue'
import TableRow from '~/components/Table/TableRow.vue'
import TableBody from '~/components/Table/TableBody.vue'
import TableHead from '~/components/Table/TableHead.vue'
import Pagination from '~/components/Pagination.vue'
import type { PaginationTarget } from '~/composables/usePagination'
import type { FilteredSpecialEvent } from '~/queries/useBlockSpecialEventsQuery'
import type { BakingRewardsSpecialEvent, PageInfo } from '~/types/generated'

type Props = {
	data: FilteredSpecialEvent<BakingRewardsSpecialEvent>
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
	goToSubPage: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
