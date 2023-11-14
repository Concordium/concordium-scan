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
					<div>
						<span class="legend"></span>
						Validator:
						<Amount
							:amount="totalAmounts.baker"
							class="pt-0 pl-2"
							:show-symbol="true"
						/>
					</div>
					<div v-if="reward.bakerReward.delegatorsAmount">
						<span class="legend legend-green"></span>
						Delegators:
						<Amount
							:amount="totalAmounts.delegators"
							class="pt-0 pl-2"
							:show-symbol="true"
						/>
					</div>
				</template>
				<Amount :amount="totalAmounts.total" />

				<FillBar>
					<FillBarItem
						:width="
							calculatePercentage(
								reward.bakerReward.bakerAmount,
								reward.bakerReward.totalAmount
							)
						"
					/>
					<FillBarItem
						:width="
							calculatePercentage(
								reward.bakerReward.delegatorsAmount,
								reward.bakerReward.totalAmount
							)
						"
						class="bar-green"
					/>
				</FillBar>
			</Tooltip>

			<LinkButton
				v-if="!isOpen"
				:on-click="() => toggleOpenState()"
				class="mt-2"
			>
				Show more
			</LinkButton>
		</TableTd>
	</TableRow>

	<TableRow v-if="isOpen">
		<TableTd v-if="breakpoint >= Breakpoint.SM"></TableTd>
		<TableTd v-if="breakpoint >= Breakpoint.MD"></TableTd>
		<TableTd :colspan="breakpoint >= Breakpoint.SM ? 1 : 3" align="right">
			<div class="block">
				<DescriptionList class="text-sm text-theme-faded">
					<DescriptionListItem>
						<span class="float-left inline-block pt-2">Block rewards</span>
						<template #content>
							<BakerDetailsPoolAmounts :amounts="reward.bakerReward" />
						</template>
					</DescriptionListItem>

					<DescriptionListItem v-if="reward.finalizationReward.totalAmount">
						<span class="float-left inline-block pt-2"
							>Finalization rewards</span
						>
						<template #content>
							<BakerDetailsPoolAmounts :amounts="reward.finalizationReward" />
						</template>
					</DescriptionListItem>

					<DescriptionListItem>
						<span class="float-left inline-block pt-2">Transaction fees</span>
						<template #content>
							<BakerDetailsPoolAmounts :amounts="reward.transactionFees" />
						</template>
					</DescriptionListItem>
				</DescriptionList>

				<LinkButton v-if="isOpen" :on-click="() => toggleOpenState()">
					Show less
				</LinkButton>
			</div>
		</TableTd>
	</TableRow>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import BakerDetailsPoolAmounts from './BakerDetailsPoolAmounts.vue'
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
import LinkButton from '~/components/atoms/LinkButton.vue'
import BlockLink from '~/components/molecules/BlockLink.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableRow from '~/components/Table/TableRow.vue'
import type { PaydayPoolReward } from '~/types/generated'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

const isOpen = ref(false)

const toggleOpenState = () => {
	isOpen.value = !isOpen.value
}

type Props = {
	reward: PaydayPoolReward
}

const props = defineProps<Props>()

const totalAmounts = {
	baker:
		props.reward.bakerReward.bakerAmount +
		props.reward.finalizationReward.bakerAmount +
		props.reward.transactionFees.bakerAmount,
	delegators:
		props.reward.bakerReward.delegatorsAmount +
		props.reward.finalizationReward.delegatorsAmount +
		props.reward.transactionFees.delegatorsAmount,
	total:
		props.reward.bakerReward.totalAmount +
		props.reward.finalizationReward.totalAmount +
		props.reward.transactionFees.totalAmount,
}
</script>

<style scoped>
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
