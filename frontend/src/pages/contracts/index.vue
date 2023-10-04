<template>
	<div>
		<Title>CCDScan | Contracts</Title>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Address</TableTh>
					<TableTh>Age</TableTh>
					<TableTh align="right">
						Creator
						<Tooltip
							text="Account address of contract creator"
							position="bottom"
							x="50%"
							y="50%"
							tooltip-position="absolute"
						>
							<span style="padding-left: 10px;">?</span>
						</Tooltip>
					</TableTh>
					<TableTh align="right">Initial Transaction</TableTh>
					<TableTh align="right">
						Module
						<Tooltip
							text="Container which holds execution code for one-or-more contracts. The below references holds the current execution code of the contract."
							position="bottom"
							x="50%"
							y="50%"
							tooltip-position="absolute"
						>
							<span style="padding-left: 10px;">?</span>
						</Tooltip>
					</TableTh>
					<TableTh align="right"
						>Balance <span class="text-theme-faded">(Ͼ)</span></TableTh
					>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="contract in data?.contracts.edges"
					:key="contract.node.contractAddress"
				>
					<TableTd>
						<ContractLink
							:address="contract.node.contractAddress"
							:contract-address-index="contract.node.contractAddressIndex"
							:contract-address-sub-index="
								contract.node.contractAddressSubIndex
							"
						/>
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
						<ModuleLink :module-reference="contract.node.moduleReference" />
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
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import Pagination from '~/components/Pagination.vue'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import { useContractsListQuery } from '~~/src/queries/useContractsListQuery'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import ModuleLink from '~~/src/components/molecules/ModuleLink.vue'

const { NOW } = useDateNow()
const { first, last, after, before, goToPage } = usePagination()
const { data } = useContractsListQuery({ first, last, after, before })
</script>
