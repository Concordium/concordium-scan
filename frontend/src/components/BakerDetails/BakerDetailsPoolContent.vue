<template>
	<div v-if="baker.state.__typename === 'ActiveBakerState' && baker.state.pool">
		<BakerDetailsHeader :baker="baker" />
		<DrawerContent>
			<div class="grid gap-8 sm:grid-cols-2 lg:grid-cols-3 mb-8">
				<DetailsCard class="sm:col-span-2 lg:col-span-3">
					<template #title>Total stake</template>
					<template #default>
						<Tooltip
							:text="`${formatPercentage(
								baker.state.pool.totalStakePercentage
							)}% of all CCD in existence`"
							class="text-theme-white"
						>
							<Amount
								:amount="baker.state.pool.totalStake"
								data-testid="total-balance"
								:show-symbol="true"
							/>
						</Tooltip>
					</template>
					<template #secondary>
						<div class="text-sm">
							<div class="whitespace-nowrap">
								<Amount
									:amount="baker.state.stakedAmount"
									class="text-theme-faded text-right whitespace-nowrap w-48"
									data-testid="locked-amount"
									:show-symbol="true"
								/>
								<div class="inline">
									<Chip class="inline-block ml-4 px-0" variant="secondary">
										Validator
									</Chip>
								</div>
							</div>
							<div class="whitespace-nowrap">
								<Amount
									:amount="baker.state.pool.delegatedStake"
									class="text-theme-faded text-right w-48 whitespace-nowrap"
									data-testid="locked-amount"
									:show-symbol="true"
								/>
								<div class="inline">
									<Chip class="inline ml-4 px-0" variant="secondary">
										Delegated
									</Chip>
								</div>
							</div>
						</div>
					</template>
				</DetailsCard>

				<DetailsCard>
					<template #title>Account</template>
					<template #default>
						<AccountLink :address="baker.account.address.asString" />
					</template>
				</DetailsCard>

				<DetailsCard v-if="computedBadgeOptions">
					<template #title>Delegation pool status</template>
					<template #default>
						<StatusCircle
							:class="[
								'h-4 inline mr-2 text-theme-interactive align-text-top',
								{
									'text-theme-info': computedBadgeOptions[0] === 'info',
									'text-theme-error': computedBadgeOptions[0] === 'failure',
								},
							]"
						/>
						{{ computedBadgeOptions[1] }}
					</template>
				</DetailsCard>

				<DetailsCard v-if="baker.state.pool.rankingByTotalStake">
					<template #title>Validator rank</template>
					<template #default>
						# {{ baker.state.pool.rankingByTotalStake.rank
						}}<span class="text-theme-faded text-sm">
							/{{ baker.state.pool.rankingByTotalStake.total }}
						</span>
					</template>
				</DetailsCard>
			</div>

			<div
				class="grid gap-8 grid-cols-2 2xl:grid-cols-4 mb-16 commission-rates rounded-lg px-8 py-4"
			>
				<BakerDetailsPoolAPY
					:apy7days="baker.state.pool.apy7days"
					:apy30days="baker.state.pool.apy30days"
				/>

				<DetailsCard v-if="baker.state.pool.lotteryPower">
					<template #title>Lottery power</template>
					<template #default>
						<span class="numerical">
							{{ formatPercentage(baker.state.pool.lotteryPower) }}%
						</span>
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Block commission</template>
					<template #default>
						<CommissionRates
							:current-payday-commission="
								baker.state.pool.paydayCommissionRates?.bakingCommission
							"
							:next-payday-commission="
								baker.state.pool.commissionRates.bakingCommission
							"
						/>
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Transaction commission</template>
					<template #default>
						<CommissionRates
							:current-payday-commission="
								baker.state.pool.paydayCommissionRates?.transactionCommission
							"
							:next-payday-commission="
								baker.state.pool.commissionRates.transactionCommission
							"
						/>
					</template>
				</DetailsCard>
			</div>

			<Accordion>
				Pay day rewards
				<template #content>
					<BakerDetailsPoolRewards
						:baker-id="baker.bakerId"
						:raw-id="baker.id"
					/>
				</template>
			</Accordion>

			<Accordion>
				Related transactions
				<template #content>
					<BakerDetailsTransactions :baker-id="baker.bakerId" />
				</template>
			</Accordion>

			<Accordion data-testid="delegators-accordion">
				Delegators
				<span class="text-theme-faded numerical ml-1">
					({{ baker.state.pool?.delegatorCount || 0 }})
				</span>
				<template #content>
					<BakerDetailsDelegators :baker-id="baker.bakerId" />
				</template>
			</Accordion>

			<Accordion v-if="baker.state.pool.metadataUrl">
				Metadata
				<template #content>
					{{ baker.state.pool.metadataUrl }}
				</template>
			</Accordion>
			<Accordion>
				Node
				<template #content>
					<div
						v-if="baker.state.nodeStatus"
						class="commission-rates rounded-lg px-8 py-4"
					>
						<NodeLink :node="baker.state.nodeStatus" />
						<span class="text-theme-faded numerical text-sm">
							{{ baker.state.nodeStatus.nodeId }}
						</span>
						<div class="grid grid-cols-3 mt-8">
							<DetailsCard>
								<template #title>Uptime</template>
								<template #default
									>{{ formatUptime(baker.state.nodeStatus.uptime, NOW) }}
								</template>
							</DetailsCard>
							<DetailsCard>
								<template #title>Client version</template>
								<template #default
									>{{ baker.state.nodeStatus.clientVersion }}
								</template>
							</DetailsCard>

							<DetailsCard>
								<template #title>Average ping</template>
								<template #default
									>{{
										baker.state.nodeStatus.averagePing
											? `${formatNumber(
													baker.state.nodeStatus.averagePing,
													2
											  )}ms`
											: '-'
									}}
								</template>
							</DetailsCard>
						</div>
					</div>
					<div v-else>
						<NotFound>
							No node status
							<template #secondary>
								Status for this node is unavailable
							</template>
						</NotFound>
					</div>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import { computed } from 'vue'
import CommissionRates from '../Staking/CommissionRates.vue'
import BakerDetailsHeader from './BakerDetailsHeader.vue'
import BakerDetailsTransactions from './BakerDetailsTransactions.vue'
import BakerDetailsDelegators from './BakerDetailsDelegators.vue'
import BakerDetailsPoolAPY from './BakerDetailsPoolAPY.vue'
import Amount from '~/components/atoms/Amount.vue'
import Chip from '~/components/atoms/Chip.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import StatusCircle from '~/components/icons/StatusCircle.vue'
import Accordion from '~/components/Accordion.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import { formatPercentage, formatNumber, formatUptime } from '~/utils/format'
import BakerDetailsPoolRewards from '~/components/BakerDetails/BakerDetailsPoolRewards.vue'
import { composeBakerStatus } from '~/utils/composeBakerStatus'
import type { BakerWithAPYFilter } from '~/queries/useBakerQuery'
import { useDateNow } from '~/composables/useDateNow'
import NodeLink from '~/components/molecules/NodeLink.vue'
import NotFound from '~/components/molecules/NotFound.vue'
type Props = {
	baker: BakerWithAPYFilter
	nextPaydayTime: string
	paydayDurationMs: number
}

const props = defineProps<Props>()
const { NOW } = useDateNow()
const computedBadgeOptions = computed(() => composeBakerStatus(props.baker))
</script>

<style scoped>
.commission-rates {
	background-color: var(--color-thead-bg);
}
</style>
