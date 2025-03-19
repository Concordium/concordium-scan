<template>
	<div>
		<SuspendedValidatorsHeader />
		<DrawerContent>
			<span class="text-theme">
				Total number: {{ passiveDelegationData.delegatorCount }}
			</span>
			<SuspendedValidators
				v-if="
					passiveDelegationData.delegators?.nodes?.length &&
					passiveDelegationData.delegators?.nodes?.length > 0
				"
				:delegators="passiveDelegationData.delegators!.nodes"
				:total-count="passiveDelegationData.delegators!.nodes.length"
				:page-info="passiveDelegationData.delegators!.pageInfo"
				:go-to-page="goToPageDelegators"
			/>
			<div v-else class="p-4">No validators suspended</div>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import SuspendedValidatorsHeader from './SuspendedValidatorsHeader.vue'
import SuspendedValidators from './SuspendedValidators.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'

import type { PassiveDelegationWithAPYFilter } from '~/queries/usePassiveDelegationQuery'
import type { PageInfo } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'

type Props = {
	passiveDelegationData: PassiveDelegationWithAPYFilter
	goToPageDelegators: (page: PageInfo) => (target: PaginationTarget) => void
	goToPageRewards: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>

<style scoped>
.commission-rates {
	background-color: var(--color-thead-bg);
}
</style>
