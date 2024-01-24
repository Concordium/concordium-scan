<template>
	<div class="inline-block">
		<Tooltip :text="tokenIdLowerCase">
			<div class="token-id truncate">
				{{ tokenIdLowerCase }}
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
const tokenIdLowerCase = computed(() => props.tokenId?.toLowerCase() || '-')
</script>
<style>
.token-id {
	max-width: 160px;
}
</style>
