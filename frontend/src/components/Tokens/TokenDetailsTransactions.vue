<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Hash</TableTh>
					<TableTh>Type</TableTh>
					<TableTh>To / From / Url</TableTh>
					<TableTh>Amount</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="tokenTxRelation in transactions"
					:key="tokenTxRelation.transaction!.transactionHash"
				>
					<TableTd class="numerical">
						<TransactionResult :result="tokenTxRelation.transaction!.result" />
						<TransactionLink
							:id="tokenTxRelation.transaction!.id"
							:hash="tokenTxRelation.transaction!.transactionHash"
						/>
					</TableTd>
					<TableTd>
						<div v-if="tokenTxRelation.data.__typename === 'CisEventDataBurn'">
							Burn
						</div>
						<div
							v-else-if="
								tokenTxRelation.data.__typename === 'CisEventDataMetadataUpdate'
							"
						>
							Metadata
						</div>
						<div
							v-else-if="tokenTxRelation.data.__typename === 'CisEventDataMint'"
						>
							Mint
						</div>
						<div
							v-else-if="
								tokenTxRelation.data.__typename === 'CisEventDataTransfer'
							"
						>
							Transfer
						</div>
					</TableTd>
					<TableTd>
						<div
							v-if="
								tokenTxRelation.data.__typename === 'CisEventDataMetadataUpdate'
							"
						>
							<TokenMetadataLink
								:data="tokenTxRelation.data.metadataUrl"
								:url="tokenTxRelation.data.metadataUrl"
							/>
						</div>
						<div
							v-else-if="
								tokenTxRelation.data.__typename === 'CisEventDataMint' ||
								tokenTxRelation.data.__typename === 'CisEventDataTransfer'
							"
						>
							To:
							<AccountLink
								v-if="tokenTxRelation.data.to.__typename === 'AccountAddress'"
								:address="tokenTxRelation.data.to.asString"
							/>
							<Contract
								v-else-if="
									tokenTxRelation.data.to.__typename === 'ContractAddress'
								"
								:address="tokenTxRelation.data.to"
							/>
						</div>
						<div
							v-if="
								tokenTxRelation.data.__typename === 'CisEventDataBurn' ||
								tokenTxRelation.data.__typename === 'CisEventDataTransfer'
							"
						>
							From:
							<AccountLink
								v-if="tokenTxRelation.data.from.__typename === 'AccountAddress'"
								:address="tokenTxRelation.data.from.asString"
							/>
							<Contract
								v-else-if="
									tokenTxRelation.data.from.__typename === 'ContractAddress'
								"
								:address="tokenTxRelation.data.from"
							/>
						</div>
					</TableTd>
					<TableTd>
						<div
							v-if="
								tokenTxRelation.data.__typename === 'CisEventDataBurn' ||
								tokenTxRelation.data.__typename === 'CisEventDataTransfer' ||
								tokenTxRelation.data.__typename === 'CisEventDataMint'
							"
						>
							<TokenAmount :amount="tokenTxRelation.data.amount" />
						</div>
						<div v-else class="text-gray-500">0</div>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination
			v-if="pageInfo && (pageInfo.hasNextPage || pageInfo.hasPreviousPage)"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import AccountLink from '~/components/molecules/AccountLink.vue'
import TokenAmount from '~/components/atoms/TokenAmount.vue'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import type { PageInfo, TokenTransaction } from '~/types/generated'
import TransactionResult from '~/components/molecules/TransactionResult.vue'
import Contract from '~/components/molecules/Contract.vue'
import TokenMetadataLink from '~/components/molecules/TokenMetadataLink.vue'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

type Props = {
	transactions: TokenTransaction[]
	pageInfo: PageInfo
	totalCount: number
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
