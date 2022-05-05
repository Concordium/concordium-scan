<template>
	<div>
		<BakerDetailsHeader :baker="baker" />
		<DrawerContent>
			<Alert
				v-if="
					baker.state.__typename === 'ActiveBakerState' &&
					baker.state.pendingChange
				"
			>
				Pending change
				<template
					v-if="
						baker.state.pendingChange.__typename === 'PendingBakerReduceStake'
					"
					#secondary
				>
					<!-- vue-tsc doesn't seem to be satisfied with the template condition ... -->
					<span
						v-if="
							baker.state.pendingChange.__typename === 'PendingBakerReduceStake'
						"
					>
						Stake will be reduced to
						{{
							convertMicroCcdToCcd(baker.state.pendingChange.newStakedAmount)
						}}
						Ͼ on {{ formatTimestamp(baker.state.pendingChange.effectiveTime) }}
					</span>
				</template>
				<template
					v-else-if="
						baker.state.pendingChange.__typename === 'PendingBakerRemoval'
					"
					#secondary
				>
					Baker will be removed
					{{ formatTimestamp(baker.state.pendingChange.effectiveTime) }}
				</template>
			</Alert>

			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<DetailsCard v-if="baker.state.__typename === 'ActiveBakerState'">
					<template #title>Staked amount</template>
					<template #default>
						<span class="numerical">
							{{ convertMicroCcdToCcd(baker.state.stakedAmount) }} Ͼ
						</span>
					</template>
					<template #secondary> {{ restakeText }} </template>
				</DetailsCard>
				<DetailsCard v-else-if="baker.state.__typename === 'RemovedBakerState'">
					<template #title>Removed at</template>
					<template #default>
						<span class="numerical">
							{{ formatTimestamp(baker.state.removedAt) }}
						</span>
					</template>
					<template #secondary>
						{{ convertTimestampToRelative(baker.state.removedAt, NOW, true) }}
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Account</template>
					<template #default>
						<AccountLink :address="baker.account.address.asString" />
					</template>
				</DetailsCard>
			</div>
			<Accordion>
				Rewards
				<template #content>
					<div
						class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0"
					>
						<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
					</div>
					<RewardMetricsForBakerChart
						:reward-metrics-data="rewardMetricsForBakerData"
						:is-loading="rewardMetricsForBakerFetching"
					/>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import { ref, computed } from 'vue'
import BakerDetailsHeader from './BakerDetailsHeader.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import Accordion from '~/components/Accordion.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Alert from '~/components/molecules/Alert.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import { useDateNow } from '~/composables/useDateNow'
import {
	convertMicroCcdToCcd,
	formatTimestamp,
	convertTimestampToRelative,
} from '~/utils/format'
import type { Baker } from '~/types/generated'
import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'
import { MetricsPeriod } from '~/types/generated'
import { useRewardMetricsForBakerQueryQuery } from '~/queries/useRewardMetricsForBakerQuery'
import RewardMetricsForBakerChart from '~/components/molecules/ChartCards/RewardMetricsForBakerChart.vue'

const { NOW } = useDateNow()

type Props = {
	baker: Baker
}

const props = defineProps<Props>()
const selectedMetricsPeriod = ref(MetricsPeriod.Last7Days)
const {
	data: rewardMetricsForBakerData,
	fetching: rewardMetricsForBakerFetching,
} = useRewardMetricsForBakerQueryQuery(
	props.baker.bakerId,
	selectedMetricsPeriod
)

const restakeText = computed(() =>
	props.baker.state.__typename === 'ActiveBakerState' &&
	props.baker.state.restakeEarnings
		? 'Earnings are being restaked'
		: 'Earnings are not being restaked'
)
</script>
