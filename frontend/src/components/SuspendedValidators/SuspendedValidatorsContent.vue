﻿<template>
	<SuspendedValidatorsHeader />
	<DrawerContent>
		<SuspendedValidators
			v-if="
				data.suspendedValidators?.nodes?.length &&
				data.suspendedValidators?.nodes?.length > 0
			"
			:suspended-validators="data.suspendedValidators!.nodes"
			:page-info="data.suspendedValidators.pageInfo"
			:go-to-page="goToPageSuspendedValidators"
		/>
		<div v-else class="p-4">No validators suspended</div>
		<PrimedForSuspensionValidators
			v-if="
				data.primedForSuspensionValidators?.nodes?.length &&
				data.primedForSuspensionValidators?.nodes?.length > 0
			"
			:primed-for-suspension-validators="data.primedForSuspensionValidators!.nodes"
			:page-info="data.primedForSuspensionValidators.pageInfo"
			:go-to-page="goToPagePrimedForSuspensionValidators"
		/>
		<div v-else class="p-4">No validators primed for suspension</div>
	</DrawerContent>
</template>

<script lang="ts" setup>
import SuspendedValidatorsHeader from './SuspendedValidatorsHeader.vue'
import SuspendedValidators from './SuspendedValidators.vue'
import PrimedForSuspensionValidators from './PrimedForSuspensionValidators.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'

import type { SuspendedValidatorsType } from '~/queries/useSuspendedValidatorsQuery'

type Props = {
	data: SuspendedValidatorsType
	goToPageSuspendedValidators: PaginationGoToPage
	goToPagePrimedForSuspensionValidators: PaginationGoToPage
}

defineProps<Props>()
</script>

<style scoped>
.commission-rates {
	background-color: var(--color-thead-bg);
}
</style>
