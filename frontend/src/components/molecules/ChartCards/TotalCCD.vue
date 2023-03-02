<template>
	<KPICardContent>
		<template #title> Total CCD in existence </template>
		<template #icon>Ͼ</template>
		<template #value>
			{{ convertMicroCcdToCcd(lastTotalMicroCcd, true) }}
		</template>
		<template #chip>
			<Tooltip
				text="Latest : Total CCD in existence"
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
	lastTotalMicroCcd?: BlockMetrics['lastTotalMicroCcd']
}

defineProps<Props>()
</script>
