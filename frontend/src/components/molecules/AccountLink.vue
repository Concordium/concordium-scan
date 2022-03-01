<template>
	<div v-if="props.address" class="inline-block">
		<LinkButton @click="drawer.push('account', null, null, props.address)">
			<UserIcon
				v-if="props.iconSize == 'big'"
				class="h-5 inline align-baseline mr-3"
			/>
			<UserIcon v-else class="h-4 text-theme-white inline align-baseline" />
			<div v-if="props.hideTooltip" text-class="text-theme-body">
				{{ shortenHash(props.address) }}
			</div>
			<Tooltip v-else :text="props.address" text-class="text-theme-body">
				{{ shortenHash(props.address) }}
			</Tooltip>
		</LinkButton>
	</div>
</template>
<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import { shortenHash } from '~/utils/format'
import { useDrawer } from '~/composables/useDrawer'
import LinkButton from '~/components/atoms/LinkButton.vue'
type Props = {
	address?: string
	iconSize?: string
	hideTooltip: boolean
}
const props = defineProps<Props>()
const drawer = useDrawer()
</script>
