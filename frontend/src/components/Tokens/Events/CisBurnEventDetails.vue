<template>
	<div>From:</div>
	<div>
		<ContractLink
			v-if="event.fromAddress.__typename === 'ContractAddress'"
			:address="event.fromAddress.asString"
			:contract-address-index="event.fromAddress.index"
			:contract-address-sub-index="event.fromAddress.subIndex"
		/>
		<AccountLink
			v-else-if="event.fromAddress.__typename === 'AccountAddress'"
			:address="event.fromAddress.asString"
		/>
	</div>
	<div>Amount:</div>
	<div>
		<TokenAmount
			:amount="event.tokenAmount"
			:symbol="symbol"
			:fraction-digits="Number(decimals || 0)"
		/>
	</div>
	<Log v-if="event.parsed" :log="event.parsed" />
</template>
<script lang="ts" setup>
import { CisBurnEvent } from '../../../../src/types/generated'
import Log from '../../Details/Log.vue'
import TokenAmount from '../../atoms/TokenAmount.vue'
import AccountLink from '../../molecules/AccountLink.vue'
import ContractLink from '../../molecules/ContractLink.vue'

type Props = {
	event: CisBurnEvent
	decimals?: number | undefined
	symbol?: string | undefined
}
defineProps<Props>()
</script>
