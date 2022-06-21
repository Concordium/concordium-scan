<template>
	<MetricCard class="pt-4">
		<NotFound
			v-if="componentState === 'empty'"
			class="mb-8 bg-theme-transparent not-found"
		>
			Payday not known
			<template #secondary>We don't know when the next payday is</template>
		</NotFound>
		<Error v-else-if="componentState === 'error'" :error="error" class="mb-8" />

		<div
			v-else-if="componentState === 'success' || componentState === 'loading'"
			class="flex flex-col items-center mb-4"
		>
			<div class="text-sm text-theme-faded w-72 text-center">Next payday</div>

			<Loader v-if="componentState === 'loading'" class="top-1/2" />

			<div
				v-else-if="componentState === 'success'"
				class="text-2xl lg:text-lg xl:text-2xl text-theme-interactive flex flex-row gap-2 items-center h-2/3"
			>
				<CalendarIcon
					class="h-6 w-6 mr-2 align-middle text-theme-interactive"
				/>

				{{ formatTimestamp(data?.paydayStatus.nextPaydayTime) }}
			</div>

			<div
				v-if="componentState === 'success'"
				class="text-md xl:text-md text-theme-white flex flex-row gap-2 items-center h-2/3"
			>
				{{
					data?.paydayStatus.nextPaydayTime > NOW.toISOString()
						? convertTimestampToRelative(
								data?.paydayStatus.nextPaydayTime,
								NOW,
								true
						  )
						: 'Imminent'
				}}
			</div>

			<div v-if="componentState === 'success'" class="flex flex-row mt-4">
				<Button
					class="text-sm"
					@blur="emitBlur"
					@click="
						() =>
							handleOnClick(
								data?.paydayStatus?.paydaySummaries?.nodes?.[0].block.blockHash
							)
					"
				>
					<BlockIcon class="h-4 w-4 mr-2 align-text-top text-theme-white" />
					Previous payday block
				</Button>
			</div>
		</div>
	</MetricCard>
</template>
<script lang="ts" setup>
import MetricCard from '~/components/atoms/MetricCard.vue'
import Button from '~/components/atoms/Button.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import BlockIcon from '~/components/icons/BlockIcon.vue'
import CalendarIcon from '~/components/icons/CalendarIcon.vue'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { useDateNow } from '~/composables/useDateNow'
import { useDrawer } from '~/composables/useDrawer'
import { usePaydayStatusQuery } from '~/queries/usePaydayStatusQuery'
import type { BlockMetrics } from '~/types/generated'

const { NOW } = useDateNow()
const drawer = useDrawer()

const { data, error, componentState } = usePaydayStatusQuery()

const emit = defineEmits(['blur'])
const emitBlur = (newTarget: FocusEvent) => {
	emit('blur', newTarget)
}

const handleOnClick = (hash?: string) => {
	hash && drawer.push({ entityTypeName: 'block', hash })
}
</script>

<style scoped>
.not-found {
	background: transparent;
}
</style>
