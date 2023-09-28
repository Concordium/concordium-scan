<template>
	<div>
		<div>Amount:</div>
		<div>
			<Amount :amount="props.contractEvent.amount" />
		</div>
	</div>
	<div>
		<div>Instigator:</div>
		<div>
			<ContractLink
				v-if="props.contractEvent.instigator.__typename === 'ContractAddress'"
				:address="props.contractEvent.instigator.asString"
				:contract-address-index="props.contractEvent.instigator.index"
				:contract-address-sub-index="props.contractEvent.instigator.subIndex"
			/>
			<AccountLink
				v-else-if="
					props.contractEvent.instigator.__typename === 'AccountAddress'
				"
				:address="props.contractEvent.instigator.asString"
			/>
		</div>
	</div>
	<div>
		<div>Receive Name:</div>
		<div>
			{{ props.contractEvent.receiveName }}
		</div>
	</div>
	<div>
		<div>Version:</div>
		<div>
			{{ props.contractEvent?.version }}
		</div>
	</div>
	<div class="w-full">
		<div>Message (HEX):</div>
		<div class="flex">
			<code class="truncate w-36">
				{{ props.contractEvent.messageAsHex }}
			</code>
			<TextCopy
				:text="props.contractEvent.messageAsHex"
				label="Click to copy message (HEX) to clipboard"
			/>
		</div>
	</div>
	<div class="w-full">
		<div>Logs (HEX):</div>
		<template v-if="props.contractEvent.eventsAsHex?.nodes?.length">
			<div
				v-for="(event, i) in props.contractEvent.eventsAsHex.nodes"
				:key="i"
				class="flex"
			>
				<code class="truncate w-36">
					{{ event }}
				</code>
				<TextCopy
					:text="event"
					label="Click to copy events logs (HEX) to clipboard"
				/>
			</div>
		</template>
	</div>
</template>
<script lang="ts" setup>
import { ContractUpdated } from '../../../../src/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import Amount from '~/components/atoms/Amount.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'

type Props = {
	contractEvent: ContractUpdated
}
const props = defineProps<Props>()
</script>
