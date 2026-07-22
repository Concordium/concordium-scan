<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />
	<LockDetailsContent
		v-else-if="componentState === 'success' && data?.lock"
		:lock="data.lock"
		:fetching="fetching"
	/>
</template>

<script lang="ts" setup>
import LockDetailsContent from './LockDetailsContent.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import { useLockQuery } from '~/queries/useLockQuery'

type Props = {
	lockId: string
}

const props = defineProps<Props>()
const lockId = toRef(props, 'lockId')

const { data, error, componentState, fetching } = useLockQuery({
	lockId,
})
</script>
