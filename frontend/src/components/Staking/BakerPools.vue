<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh width="20%">Validator ID</TableTh>
					<TableTh
						v-if="!hasPoolData && breakpoint >= Breakpoint.SM"
						width="20%"
					>
						Account
					</TableTh>
					<TableTh
						v-if="hasPoolData && breakpoint >= Breakpoint.SM"
						align="right"
					>
						Commission
					</TableTh>
					<TableTh
						v-if="hasPoolData && breakpoint >= Breakpoint.LG"
						align="right"
					>
						Validator APY <span class="text-theme-faded">(30 days)</span>
					</TableTh>
					<TableTh
						v-if="hasPoolData && breakpoint >= Breakpoint.SM"
						align="right"
					>
						Delegators APY <span class="text-theme-faded">(30 days)</span>
					</TableTh>
					<TableTh v-if="hasPoolData && breakpoint >= Breakpoint.MD">
						Delegation pool status
					</TableTh>
					<TableTh
						v-if="hasPoolData && breakpoint >= Breakpoint.XXL"
						align="right"
					>
						Delegators
					</TableTh>
					<TableTh v-if="hasPoolData" align="right">
						Available for delegation
						<span class="text-theme-faded">(Ͼ)</span></TableTh
					>
					<TableTh
						v-if="!hasPoolData || breakpoint >= Breakpoint.LG"
						align="right"
					>
						{{ hasPoolData ? 'Total stake ' : 'Staked amount ' }}
						<span class="text-theme-faded">(Ͼ)</span>
					</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="baker in data?.bakers.nodes" :key="baker.bakerId">
					<TableTd>
						<BakerLink :id="baker.bakerId" />
						<Badge
							v-if="baker.state.__typename === 'RemovedBakerState'"
							type="failure"
							class="badge ml-4"
						>
							Removed
						</Badge>
					</TableTd>

					<TableTd v-if="!hasPoolData && breakpoint >= Breakpoint.SM">
						<AccountLink :address="baker.account.address.asString" />
					</TableTd>

					<TableTd
						v-if="hasPoolData && breakpoint >= Breakpoint.SM"
						align="right"
					>
						<span
							v-if="baker.state.__typename === 'ActiveBakerState'"
							class="numerical"
						>
							{{
								formatPercentage(
									baker.state.pool?.commissionRates.bakingCommission
								)
							}}%
						</span>
					</TableTd>

					<TableTd
						v-if="hasPoolData && breakpoint >= Breakpoint.LG"
						align="right"
					>
						<span
							v-if="
								baker.state.__typename === 'ActiveBakerState' &&
								Number.isFinite(baker.state.pool?.apy.bakerApy)
							"
							class="numerical"
						>
							{{ formatPercentage(baker.state.pool!.apy.bakerApy!) }}%
						</span>
						<span v-else>-</span>
					</TableTd>

					<TableTd
						v-if="hasPoolData && breakpoint >= Breakpoint.SM"
						align="right"
					>
						<span
							v-if="
								baker.state.__typename === 'ActiveBakerState' &&
								Number.isFinite(baker.state.pool?.apy.delegatorsApy)
							"
							class="numerical"
						>
							{{ formatPercentage(baker.state.pool!.apy.delegatorsApy!) }}%
						</span>
						<span v-else>-</span>
					</TableTd>

					<TableTd v-if="hasPoolData && breakpoint >= Breakpoint.MD">
						<Badge
							v-if="
								baker.state.__typename === 'ActiveBakerState' &&
								composeBakerStatus(baker)?.[0]
							"
							:type="composeBakerStatus(baker)?.[0] || 'success'"
							class="badge"
							variant="secondary"
						>
							{{ composeBakerStatus(baker)?.[1] }}
						</Badge>
					</TableTd>

					<TableTd
						v-if="hasPoolData && breakpoint >= Breakpoint.XXL"
						align="right"
					>
						<span
							v-if="baker.state.__typename === 'ActiveBakerState'"
							class="numerical"
						>
							{{ baker.state.pool?.delegatorCount }}
						</span>
					</TableTd>

					<TableTd v-if="hasPoolData" align="right">
						<span
							v-if="baker.state.__typename === 'ActiveBakerState'"
							class="numerical"
						>
							<Tooltip
								:text="
									formatDelegationAvailableTooltip(
										baker.state.pool?.delegatedStake,
										baker.state.pool?.delegatedStakeCap
									)
								"
								class="font-sans"
							>
								<Amount
									v-if="
										baker.state.pool && baker.state.pool?.delegatedStake >= 0
									"
									:amount="
										calculateAmountAvailable(
											baker.state.pool.delegatedStake,
											baker.state.pool.delegatedStakeCap
										)
									"
								/>

								<FillBar
									v-if="baker.state.pool"
									:class="[
										calculateDelegatedStakePercent(
											baker.state.pool?.delegatedStake,
											baker.state.pool?.delegatedStakeCap
										) < 10 ||
										baker.state.pool?.delegatedStake ===
											baker.state.pool?.delegatedStakeCap
											? 'bar-warn'
											: 'bar-green-wrapper',
										,
									]"
								>
									<FillBarItem
										:width="
											calculateDelegatedStakePercent(
												baker.state.pool?.delegatedStake,
												baker.state.pool?.delegatedStakeCap
											)
										"
									/>
								</FillBar>
							</Tooltip>
						</span>
					</TableTd>

					<TableTd
						v-if="!hasPoolData || breakpoint >= Breakpoint.LG"
						class="text-right"
					>
						<Tooltip
							v-if="
								baker.state.__typename === 'ActiveBakerState' &&
								baker.state.pool
							"
							text-class="text-left"
						>
							<template #content>
								<div>
									<span class="legend"></span>
									Baker
									<span class="text-theme-faded"
										>({{
											formatPercentage(
												calculatePercentage(
													baker.state.stakedAmount,
													baker.state.pool.totalStake
												) / 100
											)
										}}%)</span
									>
								</div>
								<div v-if="baker.state.pool.delegatedStake">
									<span class="legend legend-green"></span>
									Delegators
									<span class="text-theme-faded"
										>({{
											formatPercentage(
												calculatePercentage(
													baker.state.pool.delegatedStake,
													baker.state.pool.totalStake
												) / 100
											)
										}}%)
									</span>
								</div>
							</template>
							<Amount
								v-if="
									baker.state.__typename === 'ActiveBakerState' &&
									baker.state.pool
								"
								:amount="
									baker.state.pool
										? baker.state.pool.totalStake
										: baker.state.stakedAmount
								"
							/>

							<FillBar
								v-if="
									baker.state.__typename === 'ActiveBakerState' &&
									baker.state.pool
								"
							>
								<FillBarItem
									:width="
										calculatePercentage(
											baker.state.stakedAmount,
											baker.state.pool.totalStake
										)
									"
								/>
								<FillBarItem
									:width="
										calculatePercentage(
											baker.state.pool.delegatedStake,
											baker.state.pool.totalStake
										)
									"
									class="bar-green"
								/>
							</FillBar>
						</Tooltip>
						<Amount
							v-else-if="baker.state.__typename === 'ActiveBakerState'"
							:amount="baker.state.stakedAmount"
						/>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<Pagination
			v-if="data?.bakers.pageInfo"
			:page-info="data?.bakers.pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>
<script lang="ts" setup>
import { toRef } from 'vue'
import { composeBakerStatus } from '~/utils/composeBakerStatus'
import { usePagination } from '~/composables/usePagination'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import Badge from '~/components/Badge.vue'
import Pagination from '~/components/Pagination.vue'
import Amount from '~/components/atoms/Amount.vue'
import FillBar from '~/components/atoms/FillBar.vue'
import FillBarItem from '~/components/atoms/FillBarItem.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import Table from '~/components/Table/Table.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableTh from '~/components/Table/TableTh.vue'
import TableRow from '~/components/Table/TableRow.vue'
import TableBody from '~/components/Table/TableBody.vue'
import TableHead from '~/components/Table/TableHead.vue'
import { useBakerListQuery } from '~/queries/useBakerListQuery'
import { BakerSort, BakerPoolOpenStatus } from '~/types/generated'

import { formatPercentage, calculatePercentage } from '~/utils/format'
import {
	calculateAmountAvailable,
	formatDelegationAvailableTooltip,
} from '~/utils/stakingAndDelegation'

const { breakpoint } = useBreakpoint()
const { first, last, after, before, goToPage, resetPagination } =
	usePagination()

type Props = {
	openStatusFilter: BakerPoolOpenStatus | undefined
	includeRemoved: boolean
	sort: BakerSort
}

const props = defineProps<Props>()

const sort = toRef(props, 'sort')
const openStatusFilter = toRef(props, 'openStatusFilter')
const includeRemoved = toRef(props, 'includeRemoved')

const { data } = useBakerListQuery({
	first,
	last,
	after,
	before,
	sort,
	filter: {
		openStatusFilter,
		includeRemoved,
	},
})

const hasPoolData = computed(() =>
	data.value?.bakers.nodes?.some(
		baker => baker.state.__typename === 'ActiveBakerState' && baker.state.pool
	)
)

const calculateDelegatedStakePercent = (
	delegatedStake: number,
	delegatedStakeCap: number
) => calculatePercentage(delegatedStakeCap - delegatedStake, delegatedStakeCap)

watch(
	() => [sort.value, openStatusFilter.value],
	() => resetPagination()
)
</script>

<style scoped>
/*
  These styles could have been TW classes, but are not applied correctly
  A more dynamic approach would be to have a size prop on the component
*/
.badge {
	display: inline-block;
	font-size: 0.75rem;
	padding: 0.4rem 0.5rem 0.25rem;
	margin: 0 1rem 0 0;
	line-height: 1;
}

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

.bar-green-wrapper .bar,
.bar-green {
	background-color: hsl(var(--color-interactive));
}

.bar-green-wrapper {
	background-color: hsl(var(--color-interactive-dark));
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
