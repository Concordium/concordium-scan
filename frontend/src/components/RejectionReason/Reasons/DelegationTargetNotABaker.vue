<template>
	<span> Delegation target is not a validator (id: {{ reason.bakerId }}) </span>
</template>

<script setup lang="ts">
import type { DelegationTargetNotABaker } from '~/types/generated'

type Props = {
	reason: DelegationTargetNotABaker
}

defineProps<Props>()
</script>
