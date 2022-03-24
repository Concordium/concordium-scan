<template>
	<span> <Hash :hash="reason.accountAddressString" /> is not a baker </span>
</template>

<script setup lang="ts">
import Hash from '~/components/molecules/Hash.vue'
import type { NotABaker } from '~/types/generated'

type Props = {
	reason: NotABaker
}

defineProps<Props>()
</script>
