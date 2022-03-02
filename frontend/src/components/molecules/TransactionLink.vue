<template>
	<div v-if="props.hash || props.id" class="inline-block">
		<TransactionIcon class="h-4 w-4 align-text-top" />
		<LinkButton
			class="numerical"
			@click="drawer.push('transaction', props.hash, props.id)"
		>
			<div v-if="props.hideTooltip" text-class="text-theme-body">
				{{ shortenHash(props.hash) }}
			</div>
			<Tooltip v-else :text="props.hash" text-class="text-theme-body">
				{{ shortenHash(props.hash) }}
			</Tooltip>
		</LinkButton>
	</div>
</template>
<script lang="ts" setup>
import { shortenHash } from '~/utils/format'
import { useDrawer } from '~/composables/useDrawer'
import LinkButton from '~/components/atoms/LinkButton.vue'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
type Props = {
	hash?: string
	id?: string
	hideTooltip?: boolean
}
const props = defineProps<Props>()
const drawer = useDrawer()
</script>
