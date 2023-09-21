<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Transaction Hash</TableTh>
					<TableTh>Age</TableTh>
					<TableTh>Type</TableTh>
					<TableTh>Details</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="moduleRejectEvent in moduleRejectEvents"
					:key="moduleRejectEvent"
				>
					<TableTd class="numerical">
						<TransactionLink :hash="moduleRejectEvent.transactionHash" />
					</TableTd>
					<TableTd>
						<Tooltip :text="formatTimestamp(moduleRejectEvent.blockSlotTime)">
							{{
								convertTimestampToRelative(moduleRejectEvent.blockSlotTime, NOW)
							}}
						</Tooltip>
					</TableTd>
					<TableTd>
						{{ moduleRejectEvent.rejectedEvent.__typename }}
					</TableTd>
					<TableTd>
						<div
							v-if="
								moduleRejectEvent.rejectedEvent.__typename ===
								'InvalidInitMethod'
							"
						>
							<p>Init Name:</p>
							<p>{{ moduleRejectEvent.rejectedEvent.initName }}</p>
							<p>Module Reference:</p>
							<p>
								<ModuleLink
									:module-reference="moduleRejectEvent.rejectedEvent.moduleRef"
								/>
							</p>
						</div>
						<div
							v-if="
								moduleRejectEvent.rejectedEvent.__typename ===
								'InvalidReceiveMethod'
							"
						>
							<p>Receive Name:</p>
							<p>{{ moduleRejectEvent.rejectedEvent.receiveName }}</p>
							<p>Module Reference:</p>
							<p>
								<ModuleLink
									:module-reference="moduleRejectEvent.rejectedEvent.moduleRef"
								/>
							</p>
						</div>
						<div
							v-if="
								moduleRejectEvent.rejectedEvent.__typename ===
								'ModuleHashAlreadyExists'
							"
						>
							<p>Module Reference:</p>
							<p>
								<ModuleLink
									:module-reference="moduleRejectEvent.moduleReference"
								/>
							</p>
						</div>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination v-if="pageInfo" :page-info="pageInfo" :go-to-page="goToPage" />
	</div>
</template>

<script lang="ts" setup>
import { ModuleReferenceRejectEvent, PageInfo } from '~~/src/types/generated'
import ModuleLink from '~/components/molecules/ModuleLink.vue'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import { PaginationTarget } from '~~/src/composables/usePagination'
import Pagination from '~/components/Pagination.vue'

const { NOW } = useDateNow()

type Props = {
	moduleRejectEvents: ModuleReferenceRejectEvent[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
