<template>
	<Tooltip
		text="Latest : Total CCD in circulation (Total supply - Foundation coins)"
		position="bottom"
		x="50%"
		y="66%"
	>
		<KPICardContent v-if="Number.isInteger(lastTotalMicroCcdReleased)">
			<template #title>Total circulating supply of CCD</template>
			<template #icon>Ͼ</template>
			<template #value>
				{{ convertMicroCcdToCcd(lastTotalMicroCcdReleased, false) }}
			</template>
			<template #chip>?</template>
		</KPICardContent>
	</Tooltip>
</template>
<script lang="ts" setup>
import { convertMicroCcdToCcd } from '~/utils/format'
import KPICardContent from '~/components/molecules/KPICardContent.vue'
import type { BlockMetrics } from '~/types/generated'
import Tooltip from '~/components/atoms/Tooltip.vue'

type Props = {
	lastTotalMicroCcdReleased?: BlockMetrics['lastTotalMicroCcdReleased']
}

defineProps<Props>()
</script>
