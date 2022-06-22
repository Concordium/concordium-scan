<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Account address</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.SM">
						Delegation target
					</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.MD">
						Restaking earnings?
					</TableTh>
					<TableTh align="right">Staked amount (Ͼ)</TableTh>
				</TableRow>
			</TableHead>

			<TableBody v-if="componentState === 'success'">
				<TableRow v-for="account in pagedData" :key="account.address.asString">
					<TableTd>
						<AccountLink :address="account.address.asString" />
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.SM">
						<BakerLink
							v-if="
								account.delegation?.delegationTarget.__typename ===
								'BakerDelegationTarget'
							"
							:id="account.delegation?.delegationTarget.bakerId"
						/>
						<PassiveDelegationLink v-else />
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.MD">
						<Badge
							:type="
								account.delegation?.restakeEarnings ? 'success' : 'failure'
							"
							class="badge"
							variant="secondary"
						>
							{{ account.delegation?.restakeEarnings ? 'Yes' : 'No' }}
						</Badge>
					</TableTd>

					<TableTd class="text-right">
						<Tooltip
							:text="`${formatPercentage(
								calculatePercentage(
									account.delegation?.stakedAmount,
									account.amount
								) / 100
							)}% of account balance is staked`"
						>
							<Amount :amount="account.delegation?.stakedAmount" />
						</Tooltip>
					</TableTd>
				</TableRow>
			</TableBody>

			<TableBody v-else>
				<TableRow>
					<TableTd colspan="30">
						<div v-if="componentState === 'loading'" class="relative h-48">
							<Loader />
						</div>
						<NotFound v-else-if="componentState === 'empty'">
							No delegators
							<template #secondary>
								There are currently no delegators
							</template>
						</NotFound>
						<Error v-else-if="componentState === 'error'" :error="error" />
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<LoadMore
			v-if="data?.accounts.pageInfo"
			:page-info="data?.accounts.pageInfo"
			:on-load-more="loadMore"
		/>
	</div>
</template>
<script lang="ts" setup>
import { useTopDelegatorsQuery } from '~/queries/useTopDelegatorsQuery'
import { formatPercentage, calculatePercentage } from '~/utils/format'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import Badge from '~/components/Badge.vue'
import Amount from '~/components/atoms/Amount.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import PassiveDelegationLink from '~/components/molecules/PassiveDelegationLink.vue'
import type { Account } from '~/types/generated'

const { breakpoint } = useBreakpoint()
const { pagedData, first, last, after, before, addPagedData, loadMore } =
	usePagedData<Account>()
const { data, error, componentState } = useTopDelegatorsQuery({
	first,
	last,
	after,
	before,
})

watch(
	() => data.value,
	value => {
		addPagedData(value?.accounts.nodes || [], value?.accounts.pageInfo)
	}
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
</style>
