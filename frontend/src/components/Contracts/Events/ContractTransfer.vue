<template>
	<div>Amount:</div>
	<div>
		<Amount :amount="contractEvent.amount" />
	</div>
	<div>From:</div>
	<div>
		<ContractLink
			v-if="contractEvent.from.__typename === 'ContractAddress'"
			:address="contractEvent.from.asString"
			:contract-address-index="contractEvent.from.index"
			:contract-address-sub-index="contractEvent.from.subIndex"
		/>
		<AccountLink
			v-else-if="contractEvent.from.__typename === 'AccountAddress'"
			:address="contractEvent.from.asString"
		/>
	</div>
	<div>To:</div>
	<div>
		<ContractLink
			v-if="contractEvent.to.__typename === 'ContractAddress'"
			:address="contractEvent.to.asString"
			:contract-address-index="contractEvent.to.index"
			:contract-address-sub-index="contractEvent.to.subIndex"
		/>
		<AccountLink
			v-else-if="contractEvent.to.__typename === 'AccountAddress'"
			:address="contractEvent.to.asString"
		/>
	</div>
</template>
<script lang="ts" setup>
import { Transferred } from '../../../../src/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	contractEvent: Transferred
}
const props = defineProps<Props>()
</script>
