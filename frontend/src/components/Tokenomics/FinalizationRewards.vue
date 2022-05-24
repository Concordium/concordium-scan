<template>
	<TokenomicsDisplay class="p-4 pr-0">
		<template #title>Finalisers</template>
		<template #content>
			<Table
				v-for="finalizationRewards in data.nodes"
				:key="finalizationRewards.id"
			>
				<TableHead>
					<TableRow>
						<TableTh>Finaliser</TableTh>
						<TableTh align="right">Reward (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow
						v-for="finalizer in finalizationRewards.finalizationRewards?.nodes"
						:key="finalizer.accountAddress.asString"
					>
						<TableTd>
							<AccountLink :address="finalizer.accountAddress.asString" />
						</TableTd>
						<TableTd align="right" class="numerical">
							<Amount :amount="finalizer.amount" />
						</TableTd>
					</TableRow>
				</TableBody>
				<TableFooter>
					<TableRow>
						<TableTd colspan="2" variant="with-background">
							<Pagination
								v-if="
									finalizationRewards.finalizationRewards?.pageInfo &&
									(finalizationRewards.finalizationRewards.pageInfo
										.hasNextPage ||
										finalizationRewards.finalizationRewards.pageInfo
											.hasPreviousPage)
								"
								position="relative"
								size="sm"
								:page-info="finalizationRewards.finalizationRewards.pageInfo"
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
import TableFooter from '~/components/Table/TableFooter.vue'
import Pagination from '~/components/Pagination.vue'
import type { PaginationTarget } from '~/composables/usePagination'
import type { FilteredSpecialEvent } from '~/queries/useBlockSpecialEventsQuery'
import type {
	FinalizationRewardsSpecialEvent,
	PageInfo,
} from '~/types/generated'

type Props = {
	data: FilteredSpecialEvent<FinalizationRewardsSpecialEvent>
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
	goToSubPage: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
