<template>
	<TokenomicsDisplay class="p-4 pr-0">
		<template #title>Accrued block rewards</template>
		<template #content>
			<DescriptionList v-for="event in data.nodes" :key="event.id">
				<DescriptionListItem>
					Validator
					<template #content>
						<BakerLink :id="event.bakerId" />
					</template>
				</DescriptionListItem>

				<DescriptionListItem>
					Validator reward
					<template #content>
						<Amount :amount="event.bakerReward" :show-symbol="true" />
					</template>
				</DescriptionListItem>

				<DescriptionListItem>
					Transaction fees
					<template #content>
						<Amount :amount="event.transactionFees" :show-symbol="true" />
					</template>
				</DescriptionListItem>

				<DescriptionListItem>
					Foundation charge
					<template #content>
						<Amount :amount="event.foundationCharge" :show-symbol="true" />
					</template>
				</DescriptionListItem>

				<DescriptionListItem>
					Previous gas account balance
					<template #content>
						<Amount :amount="event.oldGasAccount" :show-symbol="true" />
					</template>
				</DescriptionListItem>

				<DescriptionListItem>
					New gas account balance
					<template #content>
						<Amount :amount="event.newGasAccount" :show-symbol="true" />
					</template>
				</DescriptionListItem>
			</DescriptionList>
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
import DescriptionList from '~/components/atoms/DescriptionList.vue'
import DescriptionListItem from '~/components/atoms/DescriptionListItem.vue'
import Pagination from '~/components/Pagination.vue'
import type { PaginationTarget } from '~/composables/usePagination'
import type { PageInfo, BlockAccrueRewardSpecialEvent } from '~/types/generated'
import type { FilteredSpecialEvent } from '~/queries/useBlockSpecialEventsQuery'

type Props = {
	data: FilteredSpecialEvent<BlockAccrueRewardSpecialEvent>
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
