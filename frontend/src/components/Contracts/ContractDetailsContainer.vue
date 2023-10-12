<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<ContractDetailsContent
		v-else-if="componentState === 'success' && data?.contract"
		:contract="data?.contract"
		:pagination-events="pageOffsetInfoEvents"
		:pagination-reject-events="pageOffsetInfoRejectedEvents"
		:page-dropdown-events="pageDropdownEvents"
		:page-dropdown-rejected-events="pageDropdownRejectedEvents"
	/>
</template>

<script lang="ts" setup>
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import ContractDetailsContent from '~/components/Contracts/ContractDetailsContent.vue'
import { useContractQuery } from '~~/src/queries/useContractQuery'
import { usePaginationOffset } from '~~/src/composables/usePaginationOffset'
import { usePageDropdown } from '~~/src/composables/usePageDropdown'

type Props = {
	contractAddressIndex: number
	contractAddressSubIndex: number
}

const pageDropdownEvents = usePageDropdown();
const pageDropdownRejectedEvents = usePageDropdown();

const pageOffsetInfoEvents = usePaginationOffset(pageDropdownEvents.take);
const pageOffsetInfoRejectedEvents = usePaginationOffset(pageDropdownRejectedEvents.take);

const props = defineProps<Props>()
const contractAddressIndex = ref(props.contractAddressIndex)
const contractAddressSubIndex = ref(props.contractAddressSubIndex)

const { data, error, componentState } = useContractQuery({
	contractAddressIndex,
	contractAddressSubIndex,
	eventsVariables: {
		skip: pageOffsetInfoEvents.skip,
		take: pageOffsetInfoEvents.take
	},
	rejectEventsVariables: {
		skip: pageOffsetInfoRejectedEvents.skip,
		take: pageOffsetInfoRejectedEvents.take
	},
})
</script>
