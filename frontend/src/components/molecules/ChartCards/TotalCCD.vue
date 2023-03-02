<template>
	<Tooltip
		text="Latest : Total CCD in existence"
		position="bottom"
		x="50%"
		y="10%"
	>
		<KPICardContent>
			<template #title> Total CCD in existence </template>
			<template #icon>Ͼ</template>
			<template #value>
				{{ convertMicroCcdToCcd(lastTotalMicroCcd, true) }}
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
	lastTotalMicroCcd?: BlockMetrics['lastTotalMicroCcd']
}

defineProps<Props>()
</script>
