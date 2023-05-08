<template>
	<div class="inline-block whitespace-nowrap">
		<TransactionIcon class="h-4 w-4 align-text-top" />
		<span>
			<LinkButton
				class="numerical px-2"
				@blur="emitBlur"
				@click="() => handleOnClick()"
			>
				<div v-if="!props.tokenId">
					<span class="text-theme-body text-sm">-</span>
				</div>
				<div v-else-if="props.hideTooltip" text-class="text-theme-body">
					{{ shortenString(props.tokenId, props.length, props.suffix) }}
				</div>
				<Tooltip v-else :text="props.tokenId" text-class="text-theme-body">
					{{ shortenString(props.tokenId, props.length, props.suffix) }}
				</Tooltip>
			</LinkButton>
			<TextCopy
				:text="`${props.contractIndex}/${props.contractSubIndex}/${props.tokenId}`"
				label="Click to copy index/subindex/token id to clipboard"
				class="h-5 inline align-baseline"
				tooltip-class="font-sans"
			/>
		</span>
	</div>
</template>
<script lang="ts" setup>
import { shortenString } from '~/utils/format'
import LinkButton from '~/components/atoms/LinkButton.vue'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
import { useDrawer } from '~/composables/useDrawer'

type Props = {
	tokenId: string
	contractIndex: number
	contractSubIndex: number
	hideTooltip?: boolean
	suffix?: string
	length?: number
}
const props = defineProps<Props>()
const drawer = useDrawer()
const emit = defineEmits(['blur'])
const emitBlur = (newTarget: FocusEvent) => {
	emit('blur', newTarget)
}

const handleOnClick = () => {
	drawer.push({
		entityTypeName: 'token',
		tokenId: props.tokenId,
		contractIndex: props.contractIndex,
		contractSubIndex: props.contractSubIndex,
	})
}
</script>
