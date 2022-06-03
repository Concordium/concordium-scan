<template>
	<div>
		<PassiveDelegationHeader />
		<DrawerContent>
			<div>
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
			</div>

			<div
				class="grid gap-8 grid-cols-3 mb-16 commission-rates rounded-lg py-4"
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
					<template #title>Finalisation commission</template>
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
						v-if="
							passiveDelegationData.rewards &&
							passiveDelegationData.rewards?.nodes?.length &&
							passiveDelegationData.rewards?.nodes?.length > 0
						"
						:go-to-page="goToPageRewards"
						:page-info="passiveDelegationData.rewards!.pageInfo"
						:total-count="passiveDelegationData.rewards!.nodes.length"
						:rewards="passiveDelegationData.rewards!.nodes"
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
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { formatPercentage } from '~/utils/format'
import type { PageInfo, PassiveDelegation } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'
import Amount from '~/components/atoms/Amount.vue'
import PassiveDelegationHeader from '~/components/PassiveDelegation/PassiveDelegationHeader.vue'
import Accordion from '~/components/Accordion.vue'
import PassiveDelegationDelegators from '~/components/PassiveDelegation/PassiveDelegationDelegators.vue'
import PassiveDelegationRewards from '~/components/PassiveDelegation/PassiveDelegationRewards.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'

type Props = {
	passiveDelegationData: PassiveDelegation
	goToPageDelegators: (page: PageInfo) => (target: PaginationTarget) => void
	goToPageRewards: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
