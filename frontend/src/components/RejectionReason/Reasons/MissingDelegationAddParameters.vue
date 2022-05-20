<template>
	<span>
		A configure delegation transaction is missing one or more arguments in order
		to add a delegator
	</span>
</template>

<script setup lang="ts">
import type { MissingDelegationAddParameters } from '~/types/generated'

type Props = {
	reason: MissingDelegationAddParameters
}

defineProps<Props>()
</script>
