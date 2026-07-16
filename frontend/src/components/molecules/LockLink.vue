<template>
	<div v-if="props.lockId" class="inline-block whitespace-nowrap">
		<LockClosedIcon
			v-if="props.iconSize == 'big'"
			class="h-5 inline align-text-top mr-3"
		/>
		<LockClosedIcon
			v-else
			class="h-4 w-4 text-theme-white inline align-text-top"
		/>
		<LinkButton
			class="numerical px-2"
			@blur="emitBlur"
			@click="() => handleOnClick(props.lockId)"
		>
			<div v-if="props.hideTooltip" text-class="text-theme-body">
				{{ shortenHash(props.lockId) }}
			</div>
			<Tooltip v-else :text="props.lockId" text-class="text-theme-body">
				{{ shortenHash(props.lockId) }}
			</Tooltip>
		</LinkButton>
		<TextCopy
			:text="props.lockId"
			label="Click to copy lock ID to clipboard"
			class="h-5 inline align-baseline"
			tooltip-class="font-sans"
		/>
	</div>
</template>

<script lang="ts" setup>
import { LockClosedIcon } from '@heroicons/vue/solid/index.js'
import { shortenHash } from '~/utils/format'
import TextCopy from '~/components/atoms/TextCopy.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import LinkButton from '~/components/atoms/LinkButton.vue'
import { useDrawer } from '~/composables/useDrawer'

type Props = {
	lockId?: string | null
	iconSize?: string
	hideTooltip?: boolean
}

const props = defineProps<Props>()
const drawer = useDrawer()
const emit = defineEmits(['blur'])
const emitBlur = (newTarget: FocusEvent) => {
	emit('blur', newTarget)
}

const handleOnClick = (lockId?: string | null) => {
	if (lockId) drawer.push({ entityTypeName: 'lock', lockId })
}
</script>
