<template>
	<div>
		<Title>CCDScan | Nodes</Title>

		<header
			class="flex flex-wrap justify-between gap-8 w-full mb-4 mt-8 lg:mt-0"
		>
			<div class="flex flex-wrap flex-grow justify-end items-center gap-8">
				<NodesSortFieldSelect
					v-model="tableSortField"
					class="justify-self-start"
				/>
			</div>
		</header>

		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Node name</TableTh>
					<TableTh>Validator ID</TableTh>
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
					<TableTh v-if="breakpoint >= Breakpoint.LG" align="right">
						Rec. length
					</TableTh>
				</TableRow>
			</TableHead>
			<TableBody v-if="componentState === 'success'">
				<TableRow v-for="node in data?.nodeStatuses.nodes" :key="node.nodeId">
					<TableTd>
						<div class="whitespace-nowrap">
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
					<TableTd
						v-if="breakpoint >= Breakpoint.LG"
						class="numerical"
						align="right"
					>
						{{ node.blocksReceivedCount }}
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

		<Pagination
			v-if="
				componentState === 'success' &&
				(data?.nodeStatuses.pageInfo.hasNextPage ||
					data?.nodeStatuses.pageInfo.hasPreviousPage)
			"
			:page-info="data?.nodeStatuses.pageInfo"
			:go-to-page="goToPage"
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
import Pagination from '~/components/Pagination.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import { formatNumber, formatUptime } from '~/utils/format'
import { useDateNow } from '~/composables/useDateNow'
import { usePagination } from '~/composables/usePagination'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import { useNodeQuery } from '~/queries/useNodeQuery'
import NodeLink from '~/components/molecules/NodeLink.vue'
import NodesSortFieldSelect from '~/components/molecules/NodesSortFieldSelect.vue'
import { NodeSortField, NodeSortDirection } from '~/types/generated'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()
const { first, last, after, before, goToPage, resetPagination } =
	usePagination()
const tableSortField = ref<NodeSortField>(NodeSortField.NodeName)
const tableSortDirection = ref<NodeSortDirection>(NodeSortDirection.Asc)
const { data, error, componentState } = useNodeQuery(
	tableSortField,
	tableSortDirection,
	{
		first,
		last,
		after,
		before,
	}
)
</script>
