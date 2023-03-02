<template>
	<Tooltip text="Latest : Total unlocked CCD" position="bottom" x="50%" y="40%">
		<KPICardContent v-if="Number.isInteger(lastTotalMicroCcdUnlocked)">
			<template #title>Total unlocked CCD</template>
			<template #icon>Ͼ</template>
			<template #value>
				{{ convertMicroCcdToCcd(lastTotalMicroCcdUnlocked, true) }}
			</template>
			<template #chip> ? </template>
		</KPICardContent>
	</Tooltip>
</template>
<script lang="ts" setup>
import { convertMicroCcdToCcd } from '~/utils/format'
import KPICardContent from '~/components/molecules/KPICardContent.vue'
import type { BlockMetrics } from '~/types/generated'
import Tooltip from '~/components/atoms/Tooltip.vue'

type Props = {
	lastTotalMicroCcdUnlocked?: BlockMetrics['lastTotalMicroCcdUnlocked']
}

defineProps<Props>()
</script>
