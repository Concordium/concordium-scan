<template>
	<div>Operator:</div>
	<div>
		<ContractLink
			v-if="event.operator.__typename === 'ContractAddress'"
			:address="event.operator.asString"
			:contract-address-index="event.operator.index"
			:contract-address-sub-index="event.operator.subIndex"
		/>
		<AccountLink
			v-else-if="event.operator.__typename === 'AccountAddress'"
			:address="event.operator.asString"
		/>
	</div>
	<div>Owner:</div>
	<div>
		<ContractLink
			v-if="event.owner.__typename === 'ContractAddress'"
			:address="event.owner.asString"
			:contract-address-index="event.owner.index"
			:contract-address-sub-index="event.owner.subIndex"
		/>
		<AccountLink
			v-else-if="event.owner.__typename === 'AccountAddress'"
			:address="event.owner.asString"
		/>
	</div>
	<div>Update</div>
	<div>{{ event.update }}</div>
</template>
<script lang="ts" setup>
import { CisUpdateOperatorEvent } from '../../../../src/types/generated'
import AccountLink from '../../molecules/AccountLink.vue'
import ContractLink from '../../molecules/ContractLink.vue'

type Props = {
	event: CisUpdateOperatorEvent
	decimals: number | undefined
	symbol: string | undefined
}
defineProps<Props>()
</script>
