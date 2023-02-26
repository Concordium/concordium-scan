<template>
	<KPICardContent v-if="Number.isInteger(lastTotalMicroCcdUnlocked)">
		<template #title>Total unlocked CCD</template>
		<template #icon>Ͼ</template>
		<template #value>
			{{ convertMicroCcdToCcd(lastTotalMicroCcdUnlocked, true) }}
		</template>
		<template #chip>latest</template>
	</KPICardContent>
</template>
<script lang="ts" setup>
import { convertMicroCcdToCcd } from '~/utils/format'
import KPICardContent from '~/components/molecules/KPICardContent.vue'
import type { BlockMetrics } from '~/types/generated'

type Props = {
	lastTotalMicroCcdUnlocked?: BlockMetrics['lastTotalMicroCcdUnlocked']
}

defineProps<Props>()
</script>
