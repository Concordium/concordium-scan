<template>
	<div>
		<SuspendedValidatorsHeader />
		<DrawerContent>
			<SuspendedValidators
				v-if="
					data.suspendedValidators?.nodes?.length &&
					data.suspendedValidators?.nodes?.length > 0
				"
				:suspended-validators="data.suspendedValidators!.nodes"
				:total-count="data.suspendedValidators!.nodes.length"
				:page-info="data.suspendedValidators!.pageInfo"
				:go-to-page="goToPageDelegators"
			/>
			<div v-else class="p-4">No validators suspended</div>
			<PrimedForSuspensionValidators
				v-if="
					data.primedForSuspensionValidators?.nodes?.length &&
					data.primedForSuspensionValidators?.nodes?.length > 0
				"
				:primed-for-suspension-validators="data.primedForSuspensionValidators!.nodes"
				:total-count="data.primedForSuspensionValidators!.nodes.length"
				:page-info="data.primedForSuspensionValidators!.pageInfo"
				:go-to-page="goToPageDelegators"
			/>
			<div v-else class="p-4">No validators primed for suspension</div>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import SuspendedValidatorsHeader from './SuspendedValidatorsHeader.vue'
import SuspendedValidators from './SuspendedValidators.vue'
import PrimedForSuspensionValidators from './PrimedForSuspensionValidators.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'

import type { SuspendedValidatorsType } from '~/queries/useSuspendedValidatorsQuery'
import type { PageInfo } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'

type Props = {
	data: SuspendedValidatorsType
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
