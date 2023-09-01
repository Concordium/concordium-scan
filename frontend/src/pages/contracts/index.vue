<template>
	<div>
		<Title>CCDScan | Contracts</Title>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Address</TableTh>
					<TableTh>Creator</TableTh>
					<TableTh>Initial Transaction</TableTh>
					<TableTh>Balance <span class="text-theme-faded">(Ͼ)</span></TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="contract in data?.smartContracts.edges"
					:key="contract.node.contractAddress.asString"
				>
					<TableTd>
						<ContractLink :address="contract.node.contractAddress.asString" />
					</TableTd>
					<TableTd class="text-right">
						<AccountLink :address="contract.node.creator.asString" />
					</TableTd>
					<TableTd class="text-right">
						<TransactionLink :hash="contract.node.transactionHash" />
					</TableTd>
					<TableTd class="text-right">
						<Amount :amount="contract.node.amount" />
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination
			v-if="data?.smartContracts.pageInfo"
			:page-info="data?.smartContracts.pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import { useDateNow } from '~/composables/useDateNow'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import Pagination from '~/components/Pagination.vue'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import { useContractsListQuery } from '~~/src/queries/useContractsListQuery'

const { NOW } = useDateNow()
const { first, last, after, before, goToPage, resetPagination } =
	usePagination()
const { data } = useContractsListQuery({ first, last, after, before })
</script>
