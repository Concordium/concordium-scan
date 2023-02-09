<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<ContractDetailsContent
		v-else-if="componentState === 'success' && data"
		:contract="data"
		:go-to-page-tx="goToPageTx"
	/>
</template>

<script lang="ts" setup>
import type { Ref } from 'vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import ContractDetailsContent from '~/components/Contracts/ContractDetailsContent.vue'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
import { useContractQuery } from '~/queries/useContractQuery'

type Props = {
	address?: string
}

const props = defineProps<Props>()
const refAddress = toRef(props, 'address')
const {
	first: firstTx,
	last: lastTx,
	after: afterTx,
	before: beforeTx,
	goToPage: goToPageTx,
} = usePagination()

const transactionVariables = {
	firstTx,
	lastTx,
	afterTx,
	beforeTx,
}

const { data, error, componentState } = useContractQuery({
	address: refAddress as Ref<string>,
	transactionVariables,
})
</script>
