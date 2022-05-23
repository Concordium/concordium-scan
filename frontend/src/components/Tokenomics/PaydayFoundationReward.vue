<template>
	<TokenomicsDisplay class="p-4 pr-0">
		<template #title>Payday: Foundation reward</template>
		<template #content>
			<DescriptionList v-for="event in data.nodes" :key="event.id">
				<DescriptionListItem>
					Payday account
					<template #content>
						<AccountLink :address="event.foundationAccount.asString" />
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Development charge
					<template #content>
						<Amount :amount="event.developmentCharge" :show-symbol="true" />
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
import type {
	PageInfo,
	PaydayFoundationRewardSpecialEvent,
} from '~/types/generated'

type Props = {
	data: FilteredSpecialEvent<PaydayFoundationRewardSpecialEvent>
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
