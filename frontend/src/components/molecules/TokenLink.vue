<template>
	<div class="inline-block whitespace-nowrap">
		<TransactionIcon class="h-4 w-4 align-text-top" />
		<LinkButton
			class="numerical px-2"
			@blur="emitBlur"
			@click="() => handleOnClick()"
		>
			<div v-if="props.hideTooltip" text-class="text-theme-body">
				{{
					`${props.contractAddressIndex}/${props.contractAddressSubIndex}/${props.tokenId}`
				}}
			</div>
			<Tooltip v-else :text="props.tokenId" text-class="text-theme-body">
				{{
					`${props.contractAddressIndex}/${props.contractAddressSubIndex}/${props.tokenId}`
				}}
			</Tooltip>
		</LinkButton>
		<TextCopy
			v-if="props.url"
			:text="`${props.contractAddressIndex}/${props.contractAddressSubIndex}/${props.tokenId}`"
			label="Click to copy index/subindex/token id to clip board"
			class="h-5 inline align-baseline"
			tooltip-class="font-sans"
		/>
	</div>
</template>
<script lang="ts" setup>
import Tooltip from '../atoms/Tooltip.vue'
import LinkButton from '~/components/atoms/LinkButton.vue'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
import { useDrawer } from '~/composables/useDrawer'

type Props = {
	url?: string
	tokenId: string
	contractAddressIndex: number
	contractAddressSubIndex: number
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
		contractAddressIndex: props.contractAddressIndex,
		contractAddressSubIndex: props.contractAddressSubIndex,
	})
}
</script>
