<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<ModuleDetailsContent
		v-else-if="componentState === 'success' && data?.moduleReferenceEvent"
		:module-reference-event="data?.moduleReferenceEvent"
		:pagination-linked-contracts="pageOffsetInfoLinkedContracts"
		:pagination-linking-events="pageOffsetInfoLinkingEvents"
		:pagination-reject-events="pageOffsetInfoRejectedEvents"
		:page-dropdown-events="pageDropdownEvents"
		:page-dropdown-rejected-events="pageDropdownRejectedEvents"
		:page-dropdown-linked-contracts="pageDropdownLinkedContracts"
		:fetching="fetching"
	/>
</template>

<script lang="ts" setup>
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import ModuleDetailsContent from '~/components/Module/ModuleDetailsContent.vue'
import { useModuleReferenceEventQuery } from '~~/src/queries/useModuleQuery'

type Props = {
	moduleReference: string
}

const pageDropdownEvents = usePageDropdown()
const pageDropdownRejectedEvents = usePageDropdown()
const pageDropdownLinkedContracts = usePageDropdown()

const pageOffsetInfoLinkingEvents = usePaginationOffset(pageDropdownEvents.take)
const pageOffsetInfoRejectedEvents = usePaginationOffset(
	pageDropdownRejectedEvents.take
)
const pageOffsetInfoLinkedContracts = usePaginationOffset(
	pageDropdownLinkedContracts.take
)

const props = defineProps<Props>()
const moduleReference = ref(props.moduleReference)

const { data, error, componentState, fetching } = useModuleReferenceEventQuery({
	moduleReference,
	eventsVariables: {
		skip: pageOffsetInfoLinkingEvents.skip,
		take: pageOffsetInfoLinkingEvents.take,
	},
	rejectEventsVariables: {
		skip: pageOffsetInfoRejectedEvents.skip,
		take: pageOffsetInfoRejectedEvents.take,
	},
	linkedContract: {
		skip: pageOffsetInfoLinkedContracts.skip,
		take: pageOffsetInfoLinkedContracts.take,
	},
})
</script>
