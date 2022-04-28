<template>
	<KPICard>
		<template #title>Total CCD supply</template>
		<template #icon><CirculationIcon /></template>
		<template #value>{{ convertMicroCcdToCcd(lastTotalMicroCcd) }}</template>
		<template #chip>latest</template>
	</KPICard>
</template>
<script lang="ts" setup>
import { convertMicroCcdToCcd } from '~/utils/format'
import KPICard from '~/components/molecules/KPICard.vue'
import CirculationIcon from '~/components/icons/CirculationIcon.vue'
import type { BlockMetrics } from '~/types/generated'

type Props = {
	lastTotalMicroCcd?: BlockMetrics['lastTotalMicroCcd']
}
defineProps<Props>()
</script>
