<template>
	<TableRow>
		<TableTd>
			<Tooltip
				v-if="breakpoint >= Breakpoint.SM"
				:text="convertTimestampToRelative(reward.timestamp, NOW)"
			>
				{{ formatTimestamp(reward.timestamp) }}
			</Tooltip>
			<span v-else>
				{{ formatShortTimestamp(reward.timestamp) }}
			</span>
		</TableTd>
		<TableTd v-if="breakpoint >= Breakpoint.MD">
			<BlockLink :hash="reward.block.blockHash" />
		</TableTd>

		<TableTd align="right">
			<Tooltip text-class="text-left">
				<template #content>
					<div v-if="reward.bakerReward.totalAmount">
						<span class="legend"></span>
						Validation reward:
						<Amount
							:amount="reward.bakerReward.totalAmount"
							class="pt-0 pl-2"
							:show-symbol="true"
						/>
					</div>
					<div v-if="reward.finalizationReward.totalAmount">
						<span class="legend legend-pink"></span>
						Finalization reward:
						<Amount
							:amount="reward.finalizationReward.totalAmount"
							class="pt-0 pl-2"
							:show-symbol="true"
						/>
					</div>
					<div v-if="reward.transactionFees.totalAmount">
						<span class="legend legend-green"></span>
						Transaction fee:
						<Amount
							:amount="reward.transactionFees.totalAmount"
							class="pt-0 pl-2"
							:show-symbol="true"
						/>
					</div>
					<div v-if="!totalAmount">No rewards paid out</div>
				</template>
				<Amount :amount="totalAmount" class="pt-0" />

				<FillBar>
					<FillBarItem
						:width="
							calculatePercentage(reward.bakerReward.totalAmount, totalAmount)
						"
					/>
					<FillBarItem
						:width="
							calculatePercentage(
								reward.finalizationReward.totalAmount,
								totalAmount
							)
						"
						class="bar-pink"
					/>
					<FillBarItem
						:width="
							calculatePercentage(
								reward.transactionFees.totalAmount,
								totalAmount
							)
						"
						class="bar-green"
					/>
				</FillBar>
			</Tooltip>
		</TableTd>
	</TableRow>
</template>

<script lang="ts" setup>
import { useDateNow } from '~/composables/useDateNow'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import {
	formatTimestamp,
	formatShortTimestamp,
	calculatePercentage,
	convertTimestampToRelative,
} from '~/utils/format'
import Amount from '~/components/atoms/Amount.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import BlockLink from '~/components/molecules/BlockLink.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableRow from '~/components/Table/TableRow.vue'
import type { PaydayPoolReward } from '~/types/generated'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

type Props = {
	reward: PaydayPoolReward
}

const props = defineProps<Props>()

const totalAmount =
	props.reward.bakerReward.totalAmount +
	props.reward.finalizationReward.totalAmount +
	props.reward.transactionFees.totalAmount
</script>

<style scoped>
.bar-green {
	background-color: hsl(var(--color-interactive));
}

.bar-pink {
	background-color: hotpink;
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

.legend-pink {
	background-color: hotpink;
}
</style>
