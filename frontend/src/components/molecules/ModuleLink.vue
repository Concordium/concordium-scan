<template>
	<div v-if="props.moduleReference" class="inline-block whitespace-nowrap">
		<HashtagIcon
			v-if="props.iconSize == 'big'"
			class="h-5 inline align-text-top mr-3"
		/>
		<HashtagIcon v-else class="h-4 text-theme-white inline align-text-top" />
		<LinkButton class="numerical px-2" @blur="emitBlur" @click="handleOnClick">
			<div v-if="props.hideTooltip" text-class="text-theme-body">
				{{ shortenHash(props.moduleReference) }}
			</div>
			<Tooltip
				v-else
				:text="props.moduleReference"
				text-class="text-theme-body"
			>
				{{ shortenHash(props.moduleReference) }}
			</Tooltip>
		</LinkButton>
		<TextCopy
			:text="props.moduleReference"
			label="Click to copy reference to clipboard"
			class="h-5 inline align-baseline"
			tooltip-class="font-sans"
		/>
	</div>
</template>
<script lang="ts" setup>
import { HashtagIcon } from '@heroicons/vue/solid'
import LinkButton from '../atoms/LinkButton.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import { shortenHash } from '~/utils/format'

type Props = {
	moduleReference?: string | null
	iconSize?: string
	hideTooltip?: boolean
}

const props = defineProps<Props>()
const drawer = useDrawer()
const emit = defineEmits(['blur'])
const emitBlur = (newTarget: FocusEvent) => {
	emit('blur', newTarget)
}

const handleOnClick = () => {
	props.moduleReference &&
		drawer.push({
			entityTypeName: 'module',
			moduleReference: props.moduleReference,
		})
}
</script>
