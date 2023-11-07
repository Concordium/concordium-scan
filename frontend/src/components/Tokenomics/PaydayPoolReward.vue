<template>
	<TokenomicsDisplay class="p-4 pr-0">
		<template #title>Payday: Pool rewards</template>
		<template #content>
			<Table>
				<TableHead>
					<TableRow>
						<TableTh>Pool</TableTh>
						<TableTh align="right">Validator reward (Ͼ)</TableTh>
						<TableTh align="right">Finalization reward (Ͼ)</TableTh>
						<TableTh align="right">Transaction fees (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow v-for="rewards in data.nodes" :key="rewards.id">
						<TableTd>
							<BakerLink
								v-if="rewards.pool.__typename === 'BakerPoolRewardTarget'"
								:id="rewards.pool.bakerId"
							/>
							<span
								v-else-if="
									rewards.pool.__typename ===
									'PassiveDelegationPoolRewardTarget'
								"
							>
								<PassiveDelegationLink />
							</span>
						</TableTd>
						<TableTd align="right" class="numerical">
							<Amount :amount="rewards.bakerReward" />
						</TableTd>
						<TableTd align="right" class="numerical">
							<Amount :amount="rewards.finalizationReward" />
						</TableTd>
						<TableTd align="right" class="numerical">
							<Amount :amount="rewards.transactionFees" />
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>
			<Pagination
				v-if="data.pageInfo.hasNextPage || data.pageInfo.hasPreviousPage"
				position="relative"
				:page-info="data.pageInfo"
				:go-to-page="goToPage"
			/>
		</template>
	</TokenomicsDisplay>
</template>

<script lang="ts" setup>
import TokenomicsDisplay from './TokenomicsDisplay.vue'
import Amount from '~/components/atoms/Amount.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import Table from '~/components/Table/Table.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableTh from '~/components/Table/TableTh.vue'
import TableRow from '~/components/Table/TableRow.vue'
import TableBody from '~/components/Table/TableBody.vue'
import TableHead from '~/components/Table/TableHead.vue'
import Pagination from '~/components/Pagination.vue'
import type { FilteredSpecialEvent } from '~/queries/useBlockSpecialEventsQuery'
import type { PaginationTarget } from '~/composables/usePagination'
import type { PageInfo, PaydayPoolRewardSpecialEvent } from '~/types/generated'
import PassiveDelegationLink from '~/components/molecules/PassiveDelegationLink.vue'

type Props = {
	data: FilteredSpecialEvent<PaydayPoolRewardSpecialEvent>
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
