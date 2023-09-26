<template>
	<div class="flex flex-wrap flex-grow gap-8">
		<div>
			<p>Amount:</p>
			<p>
				<Amount :amount="props.contractEvent.amount" />
			</p>
		</div>
		<div>
			<p>Instigator:</p>
			<p>
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
			</p>
		</div>
		<div>
			<p>Receive Name:</p>
			<p>
				{{ props.contractEvent.receiveName }}
			</p>
		</div>
		<div>
			<p>Message (HEX):</p>
			<p>
				<code :class="$style.code">
					{{ props.contractEvent.messageAsHex }}
				</code>
			</p>
		</div>
		<div>
			<p>Version:</p>
			<p>
				{{ props.contractEvent.version }}
			</p>
		</div>
		<div>
			<p>Event Logs (HEX):</p>
			<ul v-if="props.contractEvent.eventsAsHex?.nodes?.length">
				<li
					v-for="(event, i) in props.contractEvent.eventsAsHex.nodes"
					:key="i"
					style="list-style-type: disc; margin: 0 0 0 20px"
				>
					<code :class="$style.code">
						{{ event }}
					</code>
				</li>
			</ul>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { ContractUpdated } from '../../../../src/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	contractEvent: ContractUpdated
}
const props = defineProps<Props>()
</script>
<style module>
.code {
	background-color: grey;
	padding: 2px 2px 0.5px;
	border-radius: 2.5px;
}
</style>
