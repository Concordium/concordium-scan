<template>
	<span>
		The {{ addressType }}
		<AccountLink
			v-if="reason.address.__typename === 'AccountAddress'"
			:address="reason.address.address"
		/>
		<Contract
			v-else-if="reason.address.__typename === 'ContractAddress'"
			:address="reason.address"
		/>
		has insufficient funds for this transaction (worth
		{{ convertMicroCcdToCcd(reason.amount) }}Ͼ)
	</span>
</template>

<script setup lang="ts">
import { convertMicroCcdToCcd } from '~/utils/format'
import Contract from '~/components/molecules/Contract.vue'
import type { AmountTooLarge } from '~/types/generated'

type Props = {
	reason: AmountTooLarge
}

const props = defineProps<Props>()

const addressType = computed(() =>
	props.reason.address.__typename === 'AccountAddress'
		? ' account'
		: ' contract'
)
</script>
