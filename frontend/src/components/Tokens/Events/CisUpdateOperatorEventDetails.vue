<template>
	<div>Operator:</div>
	<div>
		<AddressLink :address="event.operator" />
	</div>
	<div>Owner:</div>
	<div>
		<AddressLink :address="event.owner" />
	</div>
	<div>Update</div>
	<div>{{ event.update }}</div>
</template>
<script lang="ts" setup>
import type { CisUpdateOperatorEvent } from '~/types/generated'
import AddressLink from '~/components/molecules/AddressLink.vue'

type Props = {
	event: CisUpdateOperatorEvent
	decimals: number | undefined
	symbol: string | undefined
}
defineProps<Props>()
</script>
