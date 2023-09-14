<template>
	<div>
		<Title>CCDScan | Contracts</Title>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Address</TableTh>
					<TableTh>Age</TableTh>
					<TableTh align="right">Creator</TableTh>
					<TableTh align="right">Initial Transaction</TableTh>
					<TableTh align="right">Module</TableTh>
					<TableTh align="right"
						>Balance <span class="text-theme-faded">(Ͼ)</span></TableTh
					>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="contract in data?.contracts.edges"
					:key="contract.node.contractAddress.asString"
				>
					<TableTd>
						<ContractLink :address="contract.node.contractAddress.asString" />
					</TableTd>
					<TableTd>
						<Tooltip :text="formatTimestamp(contract.node.blockSlotTime)">
							{{ convertTimestampToRelative(contract.node.blockSlotTime, NOW) }}
						</Tooltip>
					</TableTd>
					<TableTd class="text-right">
						<AccountLink :address="contract.node.creator.asString" />
					</TableTd>
					<TableTd class="text-right">
						<TransactionLink :hash="contract.node.transactionHash" />
					</TableTd>
					<TableTd class="text-right">
						<Hash :hash="contract.node.moduleReference" />
					</TableTd>
					<TableTd class="text-right">
						<Amount :amount="contract.node.amount" />
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
import { useDateNow } from '~/composables/useDateNow'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import Hash from '~/components/molecules/Hash.vue'
import Pagination from '~/components/Pagination.vue'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import { useContractsListQuery } from '~~/src/queries/useContractsListQuery'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'

const { NOW } = useDateNow()
const { first, last, after, before, goToPage } = usePagination()
const { data } = useContractsListQuery({ first, last, after, before })
</script>
