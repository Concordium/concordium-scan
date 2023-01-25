<template>
	<span>Data was registered "{{ event.decoded.text }}"</span>
</template>

<script setup lang="ts">
import type { DataRegistered } from '~/types/generated'

type Props = {
	event: DataRegistered
}

defineProps<Props>()
</script>
