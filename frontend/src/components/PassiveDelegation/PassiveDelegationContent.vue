<template>
	<div>
		<PassiveDelegationHeader />
		<DrawerContent>
			<div class="grid gap-8 sm:grid-cols-2 lg:grid-cols-3 mb-8 mb-8">
				<DetailsCard>
					<template #title>Delegated stake</template>
					<template #default>
						<Tooltip
							:text="`${formatPercentage(
								passiveDelegationData.delegatedStakePercentage
							)}% of all CCD in existence`"
							class="text-theme-white"
						>
							<Amount
								:amount="passiveDelegationData.delegatedStake"
								:show-symbol="true"
							/>
						</Tooltip>
					</template>
				</DetailsCard>

				<DetailsCard>
					<template #title>APY (7 days)</template>
					<template #default>
						<span
							v-if="
								passiveDelegationData &&
								Number.isFinite(passiveDelegationData.apy7days)
							"
							class="numerical"
						>
							{{formatPercentage(passiveDelegationData.apy7days!)}}%
						</span>
						<span v-else>-</span>
					</template>
				</DetailsCard>

				<DetailsCard>
					<template #title>APY (30 days)</template>
					<template #default>
						<span
							v-if="
								passiveDelegationData &&
								Number.isFinite(passiveDelegationData.apy30days)
							"
							class="numerical"
						>
							{{formatPercentage(passiveDelegationData.apy30days!)}}%
						</span>
						<span v-else>-</span>
					</template>
				</DetailsCard>
			</div>

			<div
				class="grid gap-8 grid-cols-3 mb-16 commission-rates rounded-lg px-8 py-4"
			>
				<DetailsCard>
					<template #title>Baking commission</template>
					<template #default>
						<span class="numerical">
							{{
								formatPercentage(
									passiveDelegationData.commissionRates.bakingCommission
								)
							}}%
						</span>
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Finalization commission</template>
					<template #default>
						<span class="numerical">
							{{
								formatPercentage(
									passiveDelegationData.commissionRates.finalizationCommission
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
									passiveDelegationData.commissionRates.transactionCommission
								)
							}}%
						</span>
					</template>
				</DetailsCard>
			</div>
			<Accordion
				>Rewards
				<template #content>
					<PassiveDelegationRewards
						v-if="passiveDelegationData.poolRewards?.nodes?.length"
						:go-to-page="goToPageRewards"
						:page-info="passiveDelegationData.poolRewards!.pageInfo"
						:pool-rewards="passiveDelegationData.poolRewards!.nodes"
					/> </template
			></Accordion>
			<Accordion
				>Delegators
				<span class="text-theme-faded"
					>({{ passiveDelegationData.delegatorCount }})</span
				>
				<template #content>
					<PassiveDelegationDelegators
						v-if="
							passiveDelegationData.delegators?.nodes?.length &&
							passiveDelegationData.delegators?.nodes?.length > 0
						"
						:delegators="passiveDelegationData.delegators!.nodes"
						:total-count="passiveDelegationData.delegators!.nodes.length"
						:page-info="passiveDelegationData.delegators!.pageInfo"
						:go-to-page="goToPageDelegators"
					/>
					<div v-else class="p-4">No delegators</div>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import PassiveDelegationHeader from './PassiveDelegationHeader.vue'
import PassiveDelegationDelegators from './PassiveDelegationDelegators.vue'
import PassiveDelegationRewards from './PassiveDelegationRewards.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import Amount from '~/components/atoms/Amount.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Accordion from '~/components/Accordion.vue'

import { formatPercentage } from '~/utils/format'
import type { PassiveDelegationWithAPYFilter } from '~/queries/usePassiveDelegationQuery'
import type { PageInfo } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'

type Props = {
	passiveDelegationData: PassiveDelegationWithAPYFilter
	goToPageDelegators: (page: PageInfo) => (target: PaginationTarget) => void
	goToPageRewards: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>

<style scoped>
.commission-rates {
	background-color: var(--color-thead-bg);
}
</style>
