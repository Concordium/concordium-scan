<template>
	<span>
		Baker <BakerLink :id="event.bakerId" /> modified to
		{{
			event.restakeEarnings
				? ' restake earnings'
				: ' no longer restake earnings'
		}}
	</span>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerSetRestakeEarnings } from '~/types/generated'

type Props = {
	event: BakerSetRestakeEarnings
}

defineProps<Props>()
</script>
