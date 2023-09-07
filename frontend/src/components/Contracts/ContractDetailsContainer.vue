<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<ContractDetailsContent
		v-else-if="componentState === 'success' && data?.contract"
		:contract="data?.contract"
	/>
</template>

<script lang="ts" setup>
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import ContractDetailsContent from '~/components/Contracts/ContractDetailsContent.vue'
import { useContractQuery } from '~~/src/queries/useContractQuery'

type Props = {
	contractAddressIndex: number
	contractAddressSubIndex: number
}

const props = defineProps<Props>()
const contractAddressIndex = ref(props.contractAddressIndex)
const contractAddressSubIndex = ref(props.contractAddressSubIndex)

const { data, error, componentState } = useContractQuery({
	contractAddressIndex,
	contractAddressSubIndex,
})
</script>
