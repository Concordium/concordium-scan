<template>
	<div v-if="data?.bakerByBakerId.id">
		<BakerDetailsContent :baker="data.bakerByBakerId" />
	</div>
	<BWCubeLogoIcon
		v-else
		class="w-10 h-10 animate-ping absolute top-1/3 right-1/2"
	/>
</template>

<script lang="ts" setup>
import BakerDetailsContent from './BakerDetailsContent.vue'
import { useBakerQuery } from '~/queries/useBakerQuery'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'

type Props = {
	bakerId: number
}

const props = defineProps<Props>()

const { data } = useBakerQuery(props.bakerId)
</script>
