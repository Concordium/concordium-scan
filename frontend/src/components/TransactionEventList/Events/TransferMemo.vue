<template>
	<span>
		Memo ({{ event.decoded.decodeType }} decoded):<br />
		{{ event.decoded.text }}
	</span>
</template>

<script setup lang="ts">
import type { TransferMemo } from '~/types/generated'

type Props = {
	event: TransferMemo
}

defineProps<Props>()
</script>
