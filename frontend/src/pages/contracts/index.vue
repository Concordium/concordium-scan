<template>
	<div>
		<Title>CCDScan | Contracts</Title>

		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Address</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Age</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.SM" align="right">{{
						breakpoint >= Breakpoint.LG ? 'Transactions' : 'Txs'
					}}</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.MD" align="right">
						Owner
					</TableTh>
					<TableTh align="right">
						Balance <span class="text-theme-faded">(Ͼ)</span>
					</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="contract in data?.contracts.nodes"
					:key="contract.contractAddress.asString"
				>
					<TableTd>
						<ContractLink :address="contract.contractAddress.asString" />
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.LG">
						<Tooltip :text="formatTimestamp(contract.createdTime)">
							{{ convertTimestampToRelative(contract.createdTime, NOW) }}
						</Tooltip>
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.SM" align="right">
						<span class="numerical">
							{{ contract.transactionsCount }}
						</span>
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.MD" class="text-right">
						<AccountLink :address="contract.owner.asString" />
					</TableTd>

					<TableTd class="text-right">
						<Amount :amount="contract.balance" />
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<Pagination
			v-if="data?.contracts.pageInfo"
			:page-info="data?.contracts.pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>
<script lang="ts" setup>
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import { usePagination } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import { useContractsListQuery } from '~/queries/useContractListQuery'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()
const { first, last, after, before, goToPage, resetPagination } =
	usePagination()
const { data } = useContractsListQuery({
	first,
	last,
	after,
	before,
})
</script>
