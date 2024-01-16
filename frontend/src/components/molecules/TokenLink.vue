<template>
	<div class="inline-block whitespace-nowrap">
		<TokenIcon class="h-5 inline align-text-top" />
		<LinkButton
			class="numerical px-2"
			@blur="emitBlur"
			@click="() => handleOnClick()"
		>
			<div v-if="props.hideTooltip" text-class="text-theme-body">
				{{ props.tokenAddress }}
			</div>
			<Tooltip v-else :text="props.tokenAddress" text-class="text-theme-body">
				{{ props.tokenAddress }}
			</Tooltip>
		</LinkButton>
		<TextCopy
			:text="props.tokenAddress"
			label="Click to copy token address"
			class="h-5 inline align-baseline"
			tooltip-class="font-sans"
		/>
	</div>
</template>
<script lang="ts" setup>
import Tooltip from '../atoms/Tooltip.vue'
import TextCopy from '../atoms/TextCopy.vue'
import TokenIcon from '../icons/TokenIcon.vue'
import LinkButton from '~/components/atoms/LinkButton.vue'
import { useDrawer } from '~/composables/useDrawer'

type Props = {
	tokenAddress: string
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
