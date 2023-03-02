<template>
	<KPICardContent v-if="Number.isInteger(lastTotalMicroCcdUnlocked)">
		<template #title>Total unlocked CCD</template>
		<template #icon>Ͼ</template>
		<template #value>
			{{ convertMicroCcdToCcd(lastTotalMicroCcdUnlocked, true) }}
		</template>
		<template #chip>
			<Tooltip
				text="Latest : Total unlocked CCD"
				position="bottom"
				x="50%"
				y="50%"
				tooltip-position="absolute"
				class="text-theme-white text-right whitespace-nowrap"
			>
				<span class="text-theme-interactiveDark">?</span>
			</Tooltip>
		</template>
	</KPICardContent>
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
