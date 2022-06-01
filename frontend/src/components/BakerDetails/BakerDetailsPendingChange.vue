<template>
	<Alert>
		Pending change
		<template
			v-if="pendingChange.__typename === 'PendingBakerReduceStake'"
			#secondary
		>
			<!-- vue-tsc doesn't seem to be satisfied with the template condition ... -->
			<span v-if="pendingChange.__typename === 'PendingBakerReduceStake'">
				Stake will be reduced to
				<Amount :amount="pendingChange.newStakedAmount" :show-symbol="true" />
				in
				<Tooltip :text="pendingChange.effectiveTime">
					{{ convertTimestampToRelative(pendingChange.effectiveTime, NOW) }}
				</Tooltip>
			</span>
		</template>
		<template
			v-else-if="pendingChange.__typename === 'PendingBakerRemoval'"
			#secondary
		>
			Baker will be removed in
			<Tooltip :text="pendingChange.effectiveTime">
				{{ convertTimestampToRelative(pendingChange.effectiveTime, NOW) }}
			</Tooltip>
		</template>
	</Alert>
</template>

<script lang="ts" setup>
import Amount from '~/components/atoms/Amount.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import Alert from '~/components/molecules/Alert.vue'
import { convertTimestampToRelative } from '~/utils/format'
import { useDateNow } from '~/composables/useDateNow'
import type { PendingBakerChange } from '~/types/generated'

const { NOW } = useDateNow()

type Props = {
	pendingChange: PendingBakerChange
}

defineProps<Props>()
</script>
