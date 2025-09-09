<template>
	<div class="bg-gray rounded flex items-center gap-2">
		<span class="text-gray-500">
			Memo :
			{{ memo.decoded.text }}
		</span>
	</div>
</template>

<script setup lang="ts">
import type { Memo } from '~/types/generated'

type Props = {
	memo: Memo
}

defineProps<Props>()
</script>
