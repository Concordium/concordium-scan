<template>
	<BWCubeLogoIcon
		v-if="componentState === 'loading'"
		class="w-10 h-10 animate-ping absolute top-1/3 right-1/2"
	/>

	<DrawerContent
		v-else-if="componentState === 'empty'"
		class="flex flex-col items-center pt-20"
	>
		<InfoIcon class="h-12 w-12 mb-6 text-theme-info" />
		<h1 class="text-xl">Not found</h1>
		<h3 class="text-theme-faded">Please check the address and try again.</h3>
	</DrawerContent>

	<DrawerContent
		v-else-if="componentState === 'error'"
		class="flex flex-col items-center pt-20"
	>
		<WarningIcon class="h-12 w-12 mb-6 text-theme-error" />
		<h1 class="text-xl">Something went wrong</h1>
		<h3 class="text-theme-faded">We are very sorry. Please try again later.</h3>
		<p v-if="environment === 'dev'" class="mt-10">{{ error }}</p>
	</DrawerContent>

	<BlockDetailsContent
		v-else-if="componentState === 'success' && data"
		:block="data"
		:go-to-page-tx="goToPageTx"
		:go-to-page-finalization-rewards="goToPageFinalizationRewards"
	/>
</template>

<script lang="ts" setup>
import type { Ref } from 'vue'
import { useBlockQuery } from '~/queries/useBlockQuery'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import BlockDetailsContent from '~/components/BlockDetails/BlockDetailsContent.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import WarningIcon from '~/components/icons/WarningIcon.vue'
import InfoIcon from '~/components/icons/InfoIcon.vue'

const { environment } = useRuntimeConfig()

// transaction pagination variables
const {
	first: firstTx,
	last: lastTx,
	after: afterTx,
	before: beforeTx,
	goToPage: goToPageTx,
} = usePagination()

// finalization rewards pagination variables
const {
	first: firstFinalizationRewards,
	last: lastFinalizationRewards,
	after: afterFinalizationRewards,
	before: beforeFinalizationRewards,
	goToPage: goToPageFinalizationRewards,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const paginationVars = {
	firstTx,
	lastTx,
	afterTx,
	beforeTx,
	firstFinalizationRewards,
	lastFinalizationRewards,
	afterFinalizationRewards,
	beforeFinalizationRewards,
}

type Props = {
	id?: string
	hash?: string
}
const props = defineProps<Props>()
const refId = toRef(props, 'id')
const refHash = toRef(props, 'hash')

const { data, error, componentState } = useBlockQuery({
	id: refId as Ref<string>,
	hash: refHash as Ref<string>,
	eventsVariables: paginationVars,
})
</script>
