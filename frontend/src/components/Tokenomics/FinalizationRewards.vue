<template>
	<TokenomicsDisplay class="p-4 pr-0">
		<template #title>Finalisers</template>
		<template #content>
			<Table>
				<TableHead>
					<TableRow>
						<TableTh>Finaliser</TableTh>
						<TableTh align="right">Weight</TableTh>
						<TableTh align="right">Reward (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow v-for="finalizer in data" :key="finalizer.address.asString">
						<TableTd>
							<AccountLink :address="finalizer.address.asString" />
						</TableTd>
						<TableTd class="numerical text-right">
							{{ calculateWeight(finalizer.amount, totalAmount) }}%
						</TableTd>
						<TableTd align="right" class="numerical">
							{{ convertMicroCcdToCcd(finalizer.amount) }}
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>
			<Pagination
				v-if="pageInfo && (pageInfo.hasNextPage || pageInfo.hasPreviousPage)"
				class="relative"
				:page-info="pageInfo"
				:go-to-page="goToPage"
			/>
		</template>
	</TokenomicsDisplay>
</template>

<script lang="ts" setup>
import TokenomicsDisplay from './TokenomicsDisplay.vue'
import { convertMicroCcdToCcd, calculateWeight } from '~/utils/format'
import type { PaginationTarget } from '~/composables/usePagination'
import type { FinalizationReward, PageInfo } from '~/types/generated'

type Props = {
	data?: FinalizationReward[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

const props = defineProps<Props>()

const totalAmount =
	props.data !== undefined
		? props.data.reduce((sum, curr) => sum + curr.amount, 0)
		: 0
</script>
