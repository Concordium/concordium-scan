<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" />
	<Error v-else-if="componentState === 'error'" :error="error" />

	<TransactionDetailsContent
		v-else-if="componentState === 'success' && data"
		:transaction="data"
		:go-to-page="goToPage"
	/>
</template>

<script lang="ts" setup>
import type { Ref } from 'vue'
import TransactionDetailsContent from './TransactionDetailsContent.vue'
import { useTransactionQuery } from '~/queries/useTransactionQuery'
import { usePagination } from '~/composables/usePagination'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'

const { first, last, after, before, goToPage } = usePagination()

type Props = {
	id?: string
	hash?: string
}

const props = defineProps<Props>()
const refId = toRef(props, 'id')
const refHash = toRef(props, 'hash')

const { data, error, componentState } = useTransactionQuery({
	id: refId as Ref<string>,
	hash: refHash as Ref<string>,
	eventsVariables: {
		first,
		last,
		after,
		before,
	},
})
</script>
