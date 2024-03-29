<template>
	<TokenomicsDisplay class="p-4 pr-0">
		<template #title>Pay day: Account rewards</template>
		<template #content>
			<Table>
				<TableHead>
					<TableRow>
						<TableTh>Account</TableTh>
						<TableTh align="right">Block reward (Ͼ)</TableTh>
						<TableTh v-if="showFinalization" align="right"
							>Finalization reward (Ͼ)</TableTh
						>
						<TableTh align="right">Transaction fees (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow v-for="rewards in data.nodes" :key="rewards.id">
						<TableTd>
							<AccountLink :address="rewards.account.asString" />
						</TableTd>
						<TableTd align="right" class="numerical">
							<Amount :amount="rewards.bakerReward" />
						</TableTd>
						<TableTd v-if="showFinalization" align="right" class="numerical">
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
import AccountLink from '~/components/molecules/AccountLink.vue'
import Table from '~/components/Table/Table.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableTh from '~/components/Table/TableTh.vue'
import TableRow from '~/components/Table/TableRow.vue'
import TableBody from '~/components/Table/TableBody.vue'
import TableHead from '~/components/Table/TableHead.vue'
import Pagination from '~/components/Pagination.vue'
import type { FilteredSpecialEvent } from '~/queries/useBlockSpecialEventsQuery'
import type { PaginationTarget } from '~/composables/usePagination'
import type {
	PageInfo,
	PaydayAccountRewardSpecialEvent,
} from '~/types/generated'
import { showFinalizationFromReward } from '~~/src/utils/finalizationCommissionHelpers'

type Props = {
	data: FilteredSpecialEvent<PaydayAccountRewardSpecialEvent>
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
const props = defineProps<Props>()

const showFinalization = computed(() =>
	showFinalizationFromReward(props.data.nodes)
)
</script>
