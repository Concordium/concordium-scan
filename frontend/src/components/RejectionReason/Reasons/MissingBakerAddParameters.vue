<template>
	<span>
		A configure baker transaction is missing one or more arguments in order to
		add a baker
	</span>
</template>

<script setup lang="ts">
import type { MissingBakerAddParameters } from '~/types/generated'

type Props = {
	reason: MissingBakerAddParameters
}

defineProps<Props>()
</script>
