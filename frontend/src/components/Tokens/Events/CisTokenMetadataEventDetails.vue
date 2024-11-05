<template>
	<div>
		<div>Metadata Url:</div>
		<TokenMetadataLink :url="event.metadataUrl" />
	</div>
	<div v-if="event.hashHex">
		<div>Hash in Hexadecimal</div>
		<div>{{ event.hashHex }}</div>
	</div>
</template>
<script lang="ts" setup>
import type { CisTokenMetadataEvent } from '~/types/generated'
import TokenMetadataLink from '~/components/molecules/TokenMetadataLink.vue'

type Props = {
	event: CisTokenMetadataEvent
}
defineProps<Props>()
</script>
