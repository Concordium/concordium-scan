<template>
	<span>
		Validator <BakerLink :id="reason.bakerId" /> is already a validator
	</span>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { AlreadyABaker } from '~/types/generated'

type Props = {
	reason: AlreadyABaker
}

defineProps<Props>()
</script>
