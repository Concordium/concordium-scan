<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Contract Address</TableTh>
					<TableTh>Age</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="linkedContract in linkedContracts"
					:key="linkedContract"
				>
					<TableTd class="numerical">
						<ContractLink
							:address="linkedContract.contractAddress.asString"
							:contract-address-index="linkedContract.contractAddress.index"
							:contract-address-sub-index="
								linkedContract.contractAddress.subIndex
							"
						/>
					</TableTd>
					<TableTd>
						<Tooltip :text="formatTimestamp(linkedContract.linkedDateTime)">
							{{
								convertTimestampToRelative(linkedContract.linkedDateTime, NOW)
							}}
						</Tooltip>
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
import { LinkedContract, PageInfo } from '~~/src/types/generated'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import { PaginationTarget } from '~~/src/composables/usePagination'

const { NOW } = useDateNow()

type Props = {
	linkedContracts: LinkedContract[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
