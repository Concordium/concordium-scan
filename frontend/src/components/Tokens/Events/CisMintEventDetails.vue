<template>
	<div>To:</div>
	<div>
		<ContractLink
			v-if="event.toAddress.__typename === 'ContractAddress'"
			:address="event.toAddress.asString"
			:contract-address-index="event.toAddress.index"
			:contract-address-sub-index="event.toAddress.subIndex"
		/>
		<AccountLink
			v-else-if="event.toAddress.__typename === 'AccountAddress'"
			:address="event.toAddress.asString"
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
import { CisMintEvent } from '../../../../src/types/generated'
import Log from '../../Details/Log.vue'
import TokenAmount from '../../atoms/TokenAmount.vue'
import AccountLink from '../../molecules/AccountLink.vue'
import ContractLink from '../../molecules/ContractLink.vue'

type Props = {
	event: CisMintEvent
	decimals?: number | undefined
	symbol?: string | undefined
}
defineProps<Props>()
</script>
