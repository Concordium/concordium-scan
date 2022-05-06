<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<BakerDetailsContent
		v-else-if="componentState === 'success' && data?.bakerByBakerId.id"
		:baker="data.bakerByBakerId"
	/>
</template>

<script lang="ts" setup>
import BakerDetailsContent from './BakerDetailsContent.vue'
import { useBakerQuery } from '~/queries/useBakerQuery'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'

type Props = {
	bakerId: number
}

const props = defineProps<Props>()

const { data, error, componentState } = useBakerQuery(props.bakerId)
</script>
