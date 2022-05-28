<template>
	<div v-if="baker.state.__typename === 'ActiveBakerState' && baker.state.pool">
		<BakerDetailsHeader :baker="baker" />
		<DrawerContent>
			<BakerDetailsPendingChange
				v-if="baker.state.pendingChange"
				:pending-change="baker.state.pendingChange"
			/>

			<div class="grid gap-8 md:grid-cols-2 mb-8">
				<DetailsCard>
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
							<Amount
								:amount="baker.state.stakedAmount"
								class="text-theme-faded"
								data-testid="locked-amount"
								:show-symbol="true"
							/>
							<Chip class="inline-block ml-4 px-0" variant="secondary">
								Baker
							</Chip>
						</div>
						<div class="text-sm">
							<Amount
								:amount="baker.state.pool.delegatedStake"
								class="text-theme-faded"
								data-testid="locked-amount"
								:show-symbol="true"
							/>
							<Chip class="inline-block ml-4 px-0" variant="secondary">
								Delegated
							</Chip>
						</div>
					</template>
				</DetailsCard>

				<DetailsCard>
					<template #title>Account</template>
					<template #default>
						<AccountLink :address="baker.account.address.asString" />
					</template>
				</DetailsCard>
			</div>

			<div
				class="grid gap-8 grid-cols-2 2xl:grid-cols-4 mb-16 commission-rates rounded-lg px-8 py-4"
			>
				<DetailsCard v-if="baker.state.pool.rankingByTotalStake">
					<template #title>Baker rank</template>
					<template #default>
						# {{ baker.state.pool.rankingByTotalStake.rank
						}}<span class="text-theme-faded text-sm">
							/{{ baker.state.pool.rankingByTotalStake.total }}
						</span>
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Baking commission</template>
					<template #default>
						<span class="numerical">
							{{
								formatPercentage(
									baker.state.pool.commissionRates.bakingCommission
								)
							}}%
						</span>
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Finalisation commission</template>
					<template #default>
						<span class="numerical">
							{{
								formatPercentage(
									baker.state.pool.commissionRates.finalizationCommission
								)
							}}%
						</span>
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Transaction commission</template>
					<template #default>
						<span class="numerical">
							{{
								formatPercentage(
									baker.state.pool.commissionRates.transactionCommission
								)
							}}%
						</span>
					</template>
				</DetailsCard>
			</div>

			<Accordion>
				Rewards
				<template #content>
					<BakerDetailsRewards :baker-id="baker.bakerId" />
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
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import BakerDetailsHeader from './BakerDetailsHeader.vue'
import BakerDetailsRewards from './BakerDetailsRewards.vue'
import BakerDetailsPendingChange from './BakerDetailsPendingChange.vue'
import BakerDetailsTransactions from './BakerDetailsTransactions.vue'
import BakerDetailsDelegators from './BakerDetailsDelegators.vue'
import Amount from '~/components/atoms/Amount.vue'
import Chip from '~/components/atoms/Chip.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import Accordion from '~/components/Accordion.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import type { Baker } from '~/types/generated'

type Props = {
	baker: Baker
}

defineProps<Props>()

const formatPercentage = (num: number) => {
	return new Intl.NumberFormat(undefined, {
		minimumFractionDigits: 2,
		maximumFractionDigits: 2,
	}).format(num * 100)
}
</script>

<style scoped>
.commission-rates {
	background-color: var(--color-thead-bg);
}
</style>