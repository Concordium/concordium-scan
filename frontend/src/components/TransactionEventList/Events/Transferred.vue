<template>
	<span>
		Transferred {{ convertMicroCcdToCcd(event.amount) }}Ͼ from
		{{ fromAddressType }}
		<AccountLink
			v-if="event.from.__typename === 'AccountAddress'"
			:address="event.from.asString"
		/>
		<Contract
			v-else-if="event.from.__typename === 'ContractAddress'"
			:address="event.from"
		/>
		to {{ toAddressType }}
		<AccountLink
			v-if="event.to.__typename === 'AccountAddress'"
			:address="event.to.asString"
		/>
		<Contract
			v-else-if="event.to.__typename === 'ContractAddress'"
			:address="event.to"
		/>
	</span>
</template>

<script setup lang="ts">
import { convertMicroCcdToCcd } from '~/utils/format'
import Contract from '~/components/molecules/Contract.vue'
import type { Transferred } from '~/types/generated'

type Props = {
	event: Transferred
}

const props = defineProps<Props>()

const fromAddressType = computed(() =>
	props.event.from.__typename === 'AccountAddress' ? ' account' : ' contract'
)
const toAddressType = computed(() =>
	props.event.to.__typename === 'AccountAddress' ? ' account' : ' contract'
)
</script>
