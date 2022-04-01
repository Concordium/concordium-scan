<template>
	<a
		:href="href"
		rel="noreferrer"
		target="_blank"
		class="text-theme-interactive hover:text-theme-interactive-hover"
	>
		<Tooltip text="Open in new tab" class="text-theme-white">
			<ExternalLinkIcon stroke="none" fill="white" class="h-5 w-5 align-top" />
		</Tooltip>
		<slot />
	</a>
</template>

<script lang="ts" setup>
import ExternalLinkIcon from '~/components/icons/ExternalLinkIcon.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'

type Props = {
	href: string
}

defineProps<Props>()
</script>
