<template>
	<span>
		<Contract
			v-if="event.instigator.__typename === 'ContractAddress'"
			:address="event.instigator"
		/>
		<AccountLink
			v-else-if="event.instigator.__typename === 'AccountAddress'"
			:address="event.instigator.asString"
		/>
		updated smart contract instance
		<Contract :address="event.contractAddress" />
		<div>
			<span class="text-theme-faded"
				>Recieve Method: {{ event.receiveName }}</span
			>
		</div>
		<div v-if="event.messageAsJson">
			<span class="text-theme-faded">Parameters JSON:</span>
			<pre class="text-theme-faded" style="margin-bottom: -1em"
				>{{ JSON.stringify(JSON.parse(event.messageAsJson), null, 2) }}
		</pre
			>
		</div>
	</span>
</template>

<script setup lang="ts">
import Contract from '~/components/molecules/Contract.vue'
import type { ContractUpdated } from '~/types/generated'

type Props = {
	event: ContractUpdated
}

defineProps<Props>()
</script>
