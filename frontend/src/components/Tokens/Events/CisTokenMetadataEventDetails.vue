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
import { CisTokenMetadataEvent } from '../../../../src/types/generated'
import TokenMetadataLink from '../../molecules/TokenMetadataLink.vue'

type Props = {
	event: CisTokenMetadataEvent
}
defineProps<Props>()
</script>
