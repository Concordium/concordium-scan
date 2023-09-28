<template>
	<div>
		<div>Amount:</div>
		<div>
			<Amount :amount="props.contractEvent.contractUpdated.amount" />
		</div>
	</div>
	<div>
		<div>Contract Address:</div>
		<div>
			<ContractLink
				:address="contractEvent.contractUpdated.contractAddress.asString"
				:contract-address-index="
					contractEvent.contractUpdated.contractAddress.index
				"
				:contract-address-sub-index="
					contractEvent.contractUpdated.contractAddress.subIndex
				"
			/>
		</div>
	</div>
	<div>
		<div>Receive name of contract called:</div>
		<div>
			{{ props.contractEvent.contractUpdated.receiveName }}
		</div>
	</div>
	<div>
		<div>Version of Contract Called:</div>
		<div>
			{{ props.contractEvent?.contractUpdated.version }}
		</div>
	</div>
	<div class="w-full">
		<div>Called Message (HEX):</div>
		<div class="flex">
			<code class="truncate w-36">
				{{ props.contractEvent.contractUpdated.messageAsHex }}
			</code>
			<TextCopy
				:text="props.contractEvent.contractUpdated.messageAsHex"
				label="Click to copy message (HEX) to clipboard"
			/>
		</div>
	</div>
	<div class="w-full">
		<div>Logs (HEX) from Contract Called:</div>
		<template
			v-if="props.contractEvent.contractUpdated.eventsAsHex?.nodes?.length"
		>
			<div
				v-for="(event, i) in props.contractEvent.contractUpdated.eventsAsHex
					.nodes"
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
import { ContractCall } from '../../../../src/types/generated'
import Amount from '~/components/atoms/Amount.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'

type Props = {
	contractEvent: ContractCall
}
const props = defineProps<Props>()
</script>
