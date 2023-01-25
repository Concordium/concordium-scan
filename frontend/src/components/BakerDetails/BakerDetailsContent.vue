<template>
	<div>
		<BakerDetailsHeader :baker="baker" />
		<DrawerContent>
			<BakerDetailsPendingChange
				v-if="
					baker.state.__typename === 'ActiveBakerState' &&
					baker.state.pendingChange
				"
				:pending-change="baker.state.pendingChange"
				:next-pay-day-time="nextPayDayTime"
				:payday-duration-hrs="paydayDurationHrs"
			/>

			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<DetailsCard v-if="baker.state.__typename === 'ActiveBakerState'">
					<template #title>Staked amount</template>
					<template #default>
						<span class="numerical" data-testid="staked-amount">
							<Amount :amount="baker.state.stakedAmount" :show-symbol="true" />
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
					<BakerDetailsRewards
						:account-address="baker.account.address.asString"
						:account-id="baker.account.id"
					/>
				</template>
			</Accordion>

			<Accordion>
				Related transactions
				<template #content>
					<BakerDetailsTransactions :baker-id="baker.bakerId" />
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import { computed } from 'vue'
import BakerDetailsHeader from './BakerDetailsHeader.vue'
import BakerDetailsRewards from './BakerDetailsRewards.vue'
import BakerDetailsPendingChange from './BakerDetailsPendingChange.vue'
import BakerDetailsTransactions from './BakerDetailsTransactions.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import Accordion from '~/components/Accordion.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Amount from '~/components/atoms/Amount.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import { useDateNow } from '~/composables/useDateNow'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import type { Baker } from '~/types/generated'

const { NOW } = useDateNow()

type Props = {
	baker: Baker
	nextPayDayTime: string
	paydayDurationHrs?: number
}

const props = defineProps<Props>()

const restakeText = computed(() =>
	props.baker.state.__typename === 'ActiveBakerState' &&
	props.baker.state.restakeEarnings
		? 'Earnings are being restaked'
		: 'Earnings are not being restaked'
)
</script>
