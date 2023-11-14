<template>
	<Tooltip text-class="text-left">
		<template #content>
			<div class="text-theme-body text-sm">
				<span class="legend"></span>
				Validator:
				<Amount
					:amount="amounts.bakerAmount"
					class="pt-0 pl-2"
					:show-symbol="true"
				/>
			</div>
			<div v-if="amounts.delegatorsAmount">
				<span class="legend legend-green"></span>
				Delegators:
				<Amount
					:amount="amounts.delegatorsAmount"
					class="pt-0 pl-2"
					:show-symbol="true"
				/>
			</div>
		</template>
		<Amount :amount="amounts.totalAmount" class="pt-0" />

		<FillBar>
			<FillBarItem
				:width="calculatePercentage(amounts.bakerAmount, amounts.totalAmount)"
			/>
			<FillBarItem
				:width="
					calculatePercentage(amounts.delegatorsAmount, amounts.totalAmount)
				"
				class="bar-green"
			/>
		</FillBar>
	</Tooltip>
</template>

<script lang="ts" setup>
import { calculatePercentage } from '~/utils/format'
import Amount from '~/components/atoms/Amount.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import FillBar from '~/components/atoms/FillBar.vue'
import FillBarItem from '~/components/atoms/FillBarItem.vue'
import type { PaydayPoolRewardAmounts } from '~/types/generated'

type Props = {
	amounts: PaydayPoolRewardAmounts
}

defineProps<Props>()
</script>

<style scoped>
.bar {
	height: 4px;
	float: left;
}

.bar-warn {
	background-color: hsl(var(--color-error-dark));
}

.bar-warn .bar {
	background-color: hsl(var(--color-error));
}

.bar-green {
	background-color: hsl(var(--color-interactive));
}

.legend {
	display: inline-block;
	width: 10px;
	height: 10px;
	background-color: hsl(var(--color-info));
	margin-right: 0.5em;
}

.legend-green {
	background-color: hsl(var(--color-interactive));
}
</style>
