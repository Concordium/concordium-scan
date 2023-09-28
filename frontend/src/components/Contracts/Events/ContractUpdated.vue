<template>
	<div
		class="flex flex-wrap gap-x-16 gap-y-10 detail-container"
		:class="[{ collapsed: !isOpen }]"
	>
		<button
			class="detail-expand-btn"
			:aria-expanded="isOpen"
			:aria-controls="ID"
			@click="toggleOpenState"
		>
			<ChevronRightIcon
				:class="['h-8 transition-transform', { 'icon-open': isOpen }]"
				aria-hidden
			/>
		</button>
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
			<p>Version:</p>
			<p>
				{{ props.contractEvent.version }}
			</p>
		</div>
		<div class="w-full">
			<p>Message (HEX):</p>
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
			<p>Event Logs (HEX):</p>
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
	</div>
</template>
<script lang="ts" setup>
import { ref } from 'vue'
import { ChevronRightIcon } from '@heroicons/vue/solid/index.js'
import { ContractUpdated } from '../../../../src/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	contractEvent: ContractUpdated
}
const props = defineProps<Props>()

const isOpen = ref(false)

const toggleOpenState = () => {
	isOpen.value = !isOpen.value
}
</script>
<style>
.icon-open {
	transform: rotate(90deg);
	background-color: #787594;
}

.icon-open path {
	fill: var(--color-thead-bg);
}

.collapsed {
	max-height: 55px;
	overflow: hidden;
	transition: max-height 0.5s ease;
}

.detail-expand-btn {
	width: 40px;
	height: 40px;
	border-radius: 8px;
	background-color: var(--color-thead-bg);
	display: flex;
	justify-content: center;
	align-items: center;
	border: 1px solid #787594;
	box-shadow: 0px 0px 15px 0px rgba(0, 0, 0, 0.2);
	position: absolute;
	right: 0;
}

.detail-expand-btn[aria-expanded='true'] {
	background-color: #787594;
}

.detail-container {
	position: relative;
}
</style>
