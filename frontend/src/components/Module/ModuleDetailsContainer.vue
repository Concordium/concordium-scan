<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<ModuleDetailsContent
		v-else-if="componentState === 'success' && data?.moduleReferenceEvent"
		:module-reference-event="data?.moduleReferenceEvent"
		:go-to-page-events="goToPageEvent"
		:go-to-page-reject-events="goToPageRejectEvent"
		:go-to-page-linked-contract="goToPageLinkedContract"
	/>
</template>

<script lang="ts" setup>
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import ModuleDetailsContent from '~/components/Module/ModuleDetailsContent.vue'
import { usePagination } from '~/composables/usePagination'
import { useModuleReferenceEventQuery } from '~~/src/queries/useModuleQuery'

type Props = {
	moduleReference: string
}

const {
	first: firstEvent,
	last: lastEvent,
	after: afterEvent,
	before: beforeEvent,
	goToPage: goToPageEvent,
} = usePagination()
const {
	first: firstRejectEvent,
	last: lastRejectEvent,
	after: afterRejectEvent,
	before: beforeRejectEvent,
	goToPage: goToPageRejectEvent,
} = usePagination()
const {
	first: firstLinkedContract,
	last: lastLinkedContract,
	after: afterLinkedContract,
	before: beforeLinkedContract,
	goToPage: goToPageLinkedContract,
} = usePagination()

const props = defineProps<Props>()
const moduleReference = ref(props.moduleReference)

const { data, error, componentState } = useModuleReferenceEventQuery({
	moduleReference,
	eventsVariables: {
		first: firstEvent,
		last: lastEvent,
		after: afterEvent,
		before: beforeEvent,
	},
	rejectEventsVariables: {
		first: firstRejectEvent,
		last: lastRejectEvent,
		after: afterRejectEvent,
		before: beforeRejectEvent,
	},
	linkedContract: {
		first: firstLinkedContract,
		last: lastLinkedContract,
		after: afterLinkedContract,
		before: beforeLinkedContract,
	},
})
</script>
