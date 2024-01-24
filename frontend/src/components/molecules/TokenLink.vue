<template>
	<div class="inline-block whitespace-nowrap">
		<TokenIcon class="h-5 inline align-text-top" />
		<LinkButton class="numerical px-2" @blur="emitBlur" @click="handleOnClick">
			<Tooltip :text="tokenAddressLowerCase" text-class="text-theme-body">
				<div class="token-address truncate">
					{{ tokenAddressLowerCase }}
				</div>
			</Tooltip>
		</LinkButton>
		<TextCopy
			:text="tokenAddressLowerCase"
			label="Click to copy token address"
			class="h-5 inline align-baseline"
			tooltip-class="font-sans"
		/>
	</div>
</template>
<script lang="ts" setup>
import TextCopy from '../atoms/TextCopy.vue'
import Tooltip from '../atoms/Tooltip.vue'
import TokenIcon from '../icons/TokenIcon.vue'
import LinkButton from '~/components/atoms/LinkButton.vue'
import { useDrawer } from '~/composables/useDrawer'

type Props = {
	tokenAddress: string
	tokenId: string
	contractAddressIndex: number
	contractAddressSubIndex: number
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
const tokenAddressLowerCase = computed(() => props.tokenAddress.toLowerCase())
</script>
<style>
.token-address {
	max-width: 160px;
}
</style>
