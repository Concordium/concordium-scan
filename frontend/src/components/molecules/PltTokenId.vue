<template>
	<div class="inline-block">
		<Tooltip :text="tokenId">
			<div class="token-id truncate">
				<a
					:href="`/protocol-token/${tokenId}`"
					target="_blank"
					class="font-normal text-md text-theme-interactive flex flex-row items-center"
					>{{ tokenId }}</a
				>
			</div>
		</Tooltip>
	</div>
</template>
<script lang="ts" setup>
import Tooltip from '../atoms/Tooltip.vue'

type Props = {
	tokenId: string
}
const props = defineProps<Props>()
const tokenId = computed(() => props.tokenId || '-')
</script>
<style>
.token-id {
	max-width: 160px;
}
</style>
