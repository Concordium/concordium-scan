<template>
	<Alert>
		Pending change
		<template
			v-if="pendingChange.__typename === 'PendingBakerReduceStake'"
			#secondary
		>
			<!-- vue-tsc doesn't seem to be satisfied with the template condition ... -->
			<span v-if="pendingChange.__typename === 'PendingBakerReduceStake'">
				Validator stake will be reduced to
				<Amount :amount="pendingChange.newStakedAmount" :show-symbol="true" />
				in
				<Tooltip :text="actualTime">
					{{ convertTimestampToRelative(actualTime, NOW) }}
				</Tooltip>
			</span>
		</template>
		<template
			v-else-if="pendingChange.__typename === 'PendingBakerRemoval'"
			#secondary
		>
			Validator will be removed in
			<Tooltip :text="actualTime">
				{{ convertTimestampToRelative(actualTime, NOW) }}
			</Tooltip>
		</template>
	</Alert>
</template>

<script lang="ts" setup>
import Amount from '~/components/atoms/Amount.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import Alert from '~/components/molecules/Alert.vue'
import { convertTimestampToRelative, tillNextPayday } from '~/utils/format'
import { useDateNow } from '~/composables/useDateNow'
import type { PendingBakerChange } from '~/types/generated'

const { NOW } = useDateNow()

type Props = {
	pendingChange: PendingBakerChange
	nextPayDayTime: string
	paydayDurationMs: number
}

const props = defineProps<Props>()
const actualTime = tillNextPayday(
	props.pendingChange.effectiveTime,
	props.nextPayDayTime,
	props.paydayDurationMs
)
</script>
