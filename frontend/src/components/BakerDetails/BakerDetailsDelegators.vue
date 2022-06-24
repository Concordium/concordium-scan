<template>
	<div class="w-full">
		<Table v-if="componentState === 'success' || componentState === 'loading'">
			<TableHead>
				<TableRow>
					<TableTh>Account</TableTh>
					<TableTh align="right">Stake</TableTh>
					<TableTh>Restake earnings?</TableTh>
				</TableRow>
			</TableHead>
			<TableBody
				v-if="
					componentState === 'success' &&
					data?.bakerByBakerId?.state.__typename === 'ActiveBakerState'
				"
			>
				<TableRow
					v-for="delegator in data?.bakerByBakerId?.state?.pool?.delegators
						?.nodes || []"
					:key="delegator.accountAddress.asString"
				>
					<TableTd class="numerical">
						{{ data?.bakerByBakerId.state.stakedAmount }}
						<AccountLink :address="delegator.accountAddress.asString" />
					</TableTd>
					<TableTd align="right">
						<Amount :amount="delegator.stakedAmount" />
					</TableTd>
					<TableTd>
						<Badge
							:type="delegator.restakeEarnings ? 'success' : 'failure'"
							class="badge"
							variant="secondary"
						>
							{{ delegator?.restakeEarnings ? 'Yes' : 'No' }}
						</Badge>
					</TableTd>
				</TableRow>
			</TableBody>

			<TableBody v-else>
				<TableRow>
					<TableTd colspan="3">
						<div class="relative h-48">
							<Loader />
						</div>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<NotFound v-else-if="componentState === 'empty'">
			No delegators
			<template #secondary>
				There are no delegators for this baker pool
			</template>
		</NotFound>
		<Error v-else-if="componentState === 'error'" :error="error" />

		<Pagination
			v-if="
				componentState === 'success' &&
				(pageInfo?.hasNextPage || pageInfo?.hasPreviousPage)
			"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
import { useBakerDelegatorsQuery } from '~/queries/useBakerDelegatorsQuery'
import Badge from '~/components/Badge.vue'
import Amount from '~/components/atoms/Amount.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import Pagination from '~/components/Pagination.vue'
import Table from '~/components/Table/Table.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableTh from '~/components/Table/TableTh.vue'
import TableRow from '~/components/Table/TableRow.vue'
import TableBody from '~/components/Table/TableBody.vue'
import TableHead from '~/components/Table/TableHead.vue'
import type { Baker, PageInfo } from '~/types/generated'

const { first, last, after, before, goToPage } = usePagination({
	pageSize: PAGE_SIZE_SMALL,
})

type Props = {
	bakerId: Baker['bakerId']
}

const props = defineProps<Props>()

const { data, error, componentState } = useBakerDelegatorsQuery(props.bakerId, {
	first,
	last,
	after,
	before,
})

const pageInfo = ref<PageInfo | undefined>(
	data?.value?.bakerByBakerId?.state.__typename === 'ActiveBakerState'
		? data?.value?.bakerByBakerId?.state.pool?.delegators?.pageInfo
		: undefined
)

watch(
	() => data.value,
	value =>
		(pageInfo.value =
			value?.bakerByBakerId?.state.__typename === 'ActiveBakerState'
				? value?.bakerByBakerId?.state.pool?.delegators?.pageInfo
				: undefined)
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
