<template>
	<TokenomicsDisplay class="p-4">
		<template #title>Distributed minted CCD</template>
		<template #content>
			<DescriptionList v-for="event in data.nodes" :key="event.id">
				<DescriptionListItem>
					Baking reward account
					<template #content>
						<Amount :amount="event.bakingReward" :show-symbol="true" />
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Finalisation reward account
					<template #content>
						<Amount :amount="event.finalizationReward" :show-symbol="true" />
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Foundation account (
					<AccountLink :address="event.foundationAccountAddress.asString" /> )
					<template #content>
						<Amount
							:amount="event.platformDevelopmentCharge"
							:show-symbol="true"
						/>
					</template>
				</DescriptionListItem>
				<DescriptionListItem class="totalRow">
					TOTAL
					<template #content>
						<Amount
							:amount="
								event.platformDevelopmentCharge +
								event.finalizationReward +
								event.bakingReward
							"
							:show-symbol="true"
						/>
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
import DescriptionList from '~/components/atoms/DescriptionList.vue'
import DescriptionListItem from '~/components/atoms/DescriptionListItem.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import Pagination from '~/components/Pagination.vue'
import type { PageInfo, MintSpecialEvent } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'
import type { FilteredSpecialEvent } from '~/queries/useBlockSpecialEventsQuery'

type Props = {
	data: FilteredSpecialEvent<MintSpecialEvent>
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
