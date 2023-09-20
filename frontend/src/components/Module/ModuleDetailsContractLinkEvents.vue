<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Transaction Hash</TableTh>
					<TableTh>Contract Address</TableTh>
					<TableTh>Age</TableTh>
					<TableTh>Action</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="linkEvent in linkEvents" :key="linkEvent">
					<TableTd class="numerical">
						<TransactionLink :hash="linkEvent.transactionHash" />
					</TableTd>
					<TableTd class="numerical">
						<ContractLink
							:address="linkEvent.contractAddress.asString"
							:contract-address-index="linkEvent.contractAddress.index"
							:contract-address-sub-index="linkEvent.contractAddress.subIndex"
						/>
					</TableTd>
					<TableTd>
						<Tooltip :text="formatTimestamp(linkEvent.blockSlotTime)">
							{{ convertTimestampToRelative(linkEvent.blockSlotTime, NOW) }}
						</Tooltip>
					</TableTd>
					<TableTd>
						{{ linkEvent.linkAction }}
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination v-if="pageInfo" :page-info="pageInfo" :go-to-page="goToPage" />
	</div>
</template>

<script lang="ts" setup>
import ContractLink from '../molecules/ContractLink.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import {
	ModuleReferenceContractLinkEvent,
	PageInfo,
} from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import { PaginationTarget } from '~~/src/composables/usePagination'

const { NOW } = useDateNow()

type Props = {
	linkEvents: ModuleReferenceContractLinkEvent[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
