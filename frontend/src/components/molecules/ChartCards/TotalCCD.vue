<template>
	<KPICard>
		<template #title>Total CCD in existence</template>
		<template #icon>Ͼ</template>
		<template #value>
			{{ convertMicroCcdToCcd(lastTotalMicroCcd, true) }}
		</template>
		<template #chip>latest</template>
	</KPICard>
</template>
<script lang="ts" setup>
import { convertMicroCcdToCcd } from '~/utils/format'
import KPICard from '~/components/molecules/KPICard.vue'
import type { BlockMetrics } from '~/types/generated'

type Props = {
	lastTotalMicroCcd?: BlockMetrics['lastTotalMicroCcd']
}

defineProps<Props>()
</script>
