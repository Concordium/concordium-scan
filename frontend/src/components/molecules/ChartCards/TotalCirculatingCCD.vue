<template>
	<KPICardContent>
		<template #title>Total circulating supply of CCD</template>
		<template #icon>Ͼ</template>
		<template #value>
			{{ convertMicroCcdToCcd(lastTotalMicroCcdReleased, true) }}
		</template>
		<template #chip>latest</template>
	</KPICardContent>
</template>
<script lang="ts" setup>
import { convertMicroCcdToCcd } from '~/utils/format'
import KPICardContent from '~/components/molecules/KPICardContent.vue'
import type { BlockMetrics } from '~/types/generated'

type Props = {
	lastTotalMicroCcdReleased?: BlockMetrics['lastTotalMicroCcdReleased']
}

defineProps<Props>()
</script>
