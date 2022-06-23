<template>
	<DetailsCard>
		<template #title>Balance (Ͼ)</template>
		<template #default>
			<Amount :amount="account.amount" data-testid="total-balance" />
		</template>
		<template #secondary>
			<div v-if="account.releaseSchedule.totalAmount" class="text-sm">
				<div class="text-sm grid grid-cols-2 w-9/12">
					<Tooltip
						:text="`${calculatePercentage(
							account.releaseSchedule.totalAmount,
							account.amount
						)}% of account balance is locked`"
						class="text-theme-white text-right"
					>
						<Amount
							:amount="account.releaseSchedule.totalAmount"
							class="text-theme-faded"
							data-testid="locked-amount"
						/>
					</Tooltip>
					<div>
						<Chip class="inline-block ml-4 px-0" variant="secondary"
							>Locked</Chip
						>
					</div>
				</div>
			</div>

			<div
				v-if="
					account.baker?.state.__typename === 'ActiveBakerState' &&
					account.baker.state.stakedAmount
				"
				class="text-sm"
			>
				<div class="text-sm grid grid-cols-2 w-9/12">
					<Tooltip
						:text="`${calculatePercentage(
							account.baker.state.stakedAmount,
							account.amount
						)}% of account balance is staked`"
						class="text-theme-white text-right"
					>
						<Amount
							:amount="account.baker.state.stakedAmount"
							class="text-theme-faded"
							data-testid="staked-amount"
						/>
					</Tooltip>
					<div>
						<Chip class="inline-block ml-4 px-0 ml-2" variant="secondary">
							Staked
						</Chip>
					</div>
				</div>
			</div>
			<div
				v-if="
					account.delegation?.stakedAmount &&
					account.delegation?.stakedAmount > 0
				"
				class="text-sm"
			>
				<div class="text-sm grid grid-cols-2 w-9/12">
					<Tooltip
						:text="`${calculatePercentage(
							account.delegation?.stakedAmount,
							account.amount
						)}% of account balance is staked`"
						class="text-theme-white text-right"
					>
						<Amount
							:amount="account.delegation?.stakedAmount"
							class="text-theme-faded"
							data-testid="staked-amount"
						/>
					</Tooltip>
					<div>
						<Chip class="inline-block ml-4 px-0 ml-2" variant="secondary">
							Staked
						</Chip>
					</div>
				</div>
			</div>
		</template>
	</DetailsCard>
</template>

<script lang="ts" setup>
import DetailsCard from '~/components/DetailsCard.vue'
import Amount from '~/components/atoms/Amount.vue'
import Chip from '~/components/atoms/Chip.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import type { Account } from '~/types/generated'

type Props = {
	account: Account
}

defineProps<Props>()

const calculatePercentage = (a: number, b: number) => {
	return new Intl.NumberFormat(undefined, {
		minimumFractionDigits: 2,
		maximumFractionDigits: 2,
	}).format((a / b) * 100)
}
</script>
