<template>
	<span>
		Account <AccountLink :address="event.accountAddress.asString" /> registered
		as a validator (<BakerLink :id="event.bakerId" />) with initial stake
		{{ convertMicroCcdToCcd(event.stakedAmount) }}Ͼ
		{{ event.restakeEarnings ? ' and rewards restaked' : '' }}
	</span>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import { convertMicroCcdToCcd } from '~/utils/format'
import type { BakerAdded } from '~/types/generated'

type Props = {
	event: BakerAdded
}

defineProps<Props>()
</script>
