<template>
	<ul class="px-4">
		<li
			v-for="(event, i) in props.events"
			:key="i"
			class="border-l-4 py-4 px-6 relative"
			:class="$style.listItem"
		>
			{{ translateTransactionEvents(event) }}
		</li>
	</ul>
</template>

<script lang="ts" setup>
import { translateTransactionEvents } from '~/utils/translateTransactionEvents'
import type { TransactionSuccessfulEvent } from '~/types/transactions'

type Props = {
	events: TransactionSuccessfulEvent[]
}

const props = defineProps<Props>()
</script>

<style module>
.listItem {
	border-color: hsl(var(--color-primary));
}

/* 1: Half of it's own width, half of the border width */
.listItem::before {
	content: '';
	display: block;
	background: hsl(var(--color-primary));
	height: 1rem;
	width: 1rem;
	position: absolute;
	top: 1rem;
	left: calc(-0.5rem - 2px); /* 1 */
	border-radius: 50%;
}
</style>
