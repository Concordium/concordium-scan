<template>
	<TokenomicsDisplay class="p-4">
		<template #title>Block rewards</template>
		<template #content>
			<DescriptionList v-for="event in data.nodes" :key="event.id">
				<DescriptionListItem>
					Validator
					<template #content>
						<AccountLink :address="event.bakerAccountAddress.asString" />
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
import AccountLink from '~/components/molecules/AccountLink.vue'
import DescriptionList from '~/components/atoms/DescriptionList.vue'
import DescriptionListItem from '~/components/atoms/DescriptionListItem.vue'
import Pagination from '~/components/Pagination.vue'
import type { FilteredSpecialEvent } from '~/queries/useBlockSpecialEventsQuery'
import type { PaginationTarget } from '~/composables/usePagination'
import type { PageInfo, BlockRewardsSpecialEvent } from '~/types/generated'

type Props = {
	data: FilteredSpecialEvent<BlockRewardsSpecialEvent>
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>

<style>
.totalRow {
	border-top: solid 1px white;
	margin-top: 4px;
	padding-top: 4px;
}
</style>
