<template>
	<span>
		Account <AccountLink :address="event.accountAddressString" /> registered as
		a baker (<BakerLink :id="event.bakerId" />) with initial stake
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
