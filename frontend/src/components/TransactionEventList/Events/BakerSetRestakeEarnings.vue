<template>
	<span>
		Baker <Baker :id="event.bakerId" /> modified to
		{{
			event.restakeEarnings
				? ' restake earnings'
				: ' no longer restake earnings'
		}}
	</span>
</template>

<script setup lang="ts">
import Baker from '~/components/molecules/Baker.vue'
import type { BakerSetRestakeEarnings } from '~/types/generated'

type Props = {
	event: BakerSetRestakeEarnings
}

defineProps<Props>()
</script>
