<template>
	<div>
		<Title>CCDScan | Nodes</Title>

		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Node name</TableTh>
					<TableTh>Baker ID</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.SM">Uptime</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.SM" align="right">
						Node version
					</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.MD" align="right">
						Avg. ping
					</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG" align="right">
						Peers
					</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG" align="right">
						Fin. length
					</TableTh>
				</TableRow>
			</TableHead>
			<TableBody v-if="componentState === 'success'">
				<TableRow v-for="node in pagedData" :key="node.nodeId">
					<TableTd>
						<div class="whitespace-normal">
							<NodeLink :node="node" />
						</div>
					</TableTd>
					<TableTd>
						<BakerLink
							v-if="Number.isInteger(node.consensusBakerId)"
							:id="node.consensusBakerId"
						/>
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.SM">
						{{ formatUptime(node.uptime, NOW) }}
					</TableTd>

					<TableTd
						v-if="breakpoint >= Breakpoint.SM"
						class="numerical"
						align="right"
					>
						{{ node.clientVersion }}
					</TableTd>

					<TableTd
						v-if="breakpoint >= Breakpoint.MD"
						class="numerical"
						align="right"
					>
						{{
							node.averagePing ? `${formatNumber(node.averagePing, 2)}ms` : '-'
						}}
					</TableTd>

					<TableTd
						v-if="breakpoint >= Breakpoint.LG"
						class="numerical"
						align="right"
					>
						{{ node.peersCount }}
					</TableTd>

					<TableTd
						v-if="breakpoint >= Breakpoint.LG"
						class="numerical"
						align="right"
					>
						{{ node.finalizedBlockHeight }}
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
							No nodes
							<template #secondary>
								There are no nodes currently online
							</template>
						</NotFound>
						<Error v-else-if="componentState === 'error'" :error="error" />
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<LoadMore
			v-if="
				componentState === 'success' &&
				(data?.nodeStatuses.pageInfo.hasNextPage ||
					data?.nodeStatuses.pageInfo.hasPreviousPage)
			"
			:page-info="data?.nodeStatuses.pageInfo"
			:on-load-more="loadMore"
		/>
	</div>
</template>

<script lang="ts" setup>
import Table from '~/components/Table/Table.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableTh from '~/components/Table/TableTh.vue'
import TableRow from '~/components/Table/TableRow.vue'
import TableBody from '~/components/Table/TableBody.vue'
import TableHead from '~/components/Table/TableHead.vue'
import LoadMore from '~/components/LoadMore.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import { formatNumber, formatUptime } from '~/utils/format'
import { useDateNow } from '~/composables/useDateNow'
import { usePagedData } from '~/composables/usePagedData'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import { useNodeQuery } from '~/queries/useNodeQuery'
import type { NodeStatus } from '~/types/generated'
import NodeLink from '~/components/molecules/NodeLink.vue'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()
const { pagedData, first, last, after, before, addPagedData, loadMore } =
	usePagedData<NodeStatus>()

const { data, error, componentState } = useNodeQuery({
	first,
	last,
	after,
	before,
})

watch(
	() => data.value,
	value => {
		addPagedData(value?.nodeStatuses.nodes || [], value?.nodeStatuses.pageInfo)
	}
)
</script>
