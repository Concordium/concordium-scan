<template>
	<div>
		<NodeDetailsHeader :node="node" />
		<DrawerContent>
			<div class="grid gap-8 sm:grid-cols-2 lg:grid-cols-3 mb-8 mb-8">
				<DetailsCard>
					<template #title>Baker</template>
					<template #default
						><BakerLink
							v-if="Number.isInteger(node.consensusBakerId)"
							:id="node.consensusBakerId"
						/>
						<div v-else>-</div>
					</template>
				</DetailsCard>

				<DetailsCard>
					<template #title>Uptime</template>
					<template #default>{{ formatUptime(node.uptime, NOW) }} </template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Client version</template>
					<template #default>{{ node.clientVersion }} </template>
				</DetailsCard>

				<DetailsCard>
					<template #title>Average ping</template>
					<template #default
						>{{
							node.averagePing ? `${formatNumber(node.averagePing, 2)}ms` : '-'
						}}
					</template>
				</DetailsCard>

				<DetailsCard>
					<template #title>Packets Sent</template>
					<template #default
						>{{ node.packetsSent }} ({{
							formatBytesPerSecond(node.averageBytesPerSecondOut)
						}})
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>packetsReceived</template>
					<template #default
						>{{ node.packetsReceived }} ({{
							formatBytesPerSecond(node.averageBytesPerSecondIn)
						}})</template
					>
				</DetailsCard>
				<DetailsCard>
					<template #title>Validation Committee</template>
					<template #default
						>{{ translateValidationCommittee(node.bakingCommitteeMember) }}
					</template>
				</DetailsCard>
			</div>

			<DescriptionList class="mb-8">
				<DescriptionListItem
					>Best Block<template #content>
						<BlockLink :hash="node.bestBlock" /></template
				></DescriptionListItem>
				<DescriptionListItem
					>Best Block Height<template #content>
						{{ node.bestBlockHeight }}</template
					></DescriptionListItem
				>
				<DescriptionListItem
					>Arrive time of Best Block<template #content>
						{{ node.bestArrivedTime }}
					</template></DescriptionListItem
				>
				<DescriptionListItem
					>Arrive time of Best Block (EMA)<template #content
						>{{ node.blockReceivePeriodEma }}
					</template></DescriptionListItem
				>
				<DescriptionListItem
					>Arrive time of Best Block (EMSD)<template #content>
						{{ node.blockReceivePeriodEmsd }}</template
					></DescriptionListItem
				>
			</DescriptionList>
			<DescriptionList class="mb-8">
				<DescriptionListItem
					>Last finalized block<template #content>
						<BlockLink :hash="node.finalizedBlock" /></template
				></DescriptionListItem>
				<DescriptionListItem
					>Last finalized block height<template #content>
						{{ node.finalizedBlockHeight }}</template
					></DescriptionListItem
				>
				<DescriptionListItem
					>Time of last finalization<template #content>
						{{ node.finalizedTime }}
					</template></DescriptionListItem
				>
				<DescriptionListItem
					>Finalization period (EMA)<template #content
						>{{ node.finalizationPeriodEma }}
					</template></DescriptionListItem
				>
				<DescriptionListItem
					>Finalization period (EMSD)<template #content
						>{{ node.finalizationPeriodEmsd }}
					</template></DescriptionListItem
				>
			</DescriptionList>
			<Accordion>
				Peers
				<span class="numerical text-theme-faded">({{ node.peersCount }})</span>
				<template #content>
					<div v-if="node.peersCount > 0">
						<div v-for="peer in node.peersList" :key="peer.nodeId">
							<NodeLink v-if="peer.nodeStatus" :node="peer.nodeStatus" />
							<div v-else class="ml-6">
								<span class="numerical">({{ peer.nodeId }})</span>
								<Tooltip text="Status for this node is unavailable">
									<WarningIcon
										class="h-5 text-theme-white inline align-text-top ml-1"
									/>
								</Tooltip>
							</div>
						</div>
					</div>
					<div v-else>
						<NotFound>
							No peers
							<template #secondary> This node has no peers </template>
						</NotFound>
					</div>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { translateValidationCommittee } from '~/utils/translateValidationCommittee'
import {
	formatNumber,
	formatUptime,
	formatBytesPerSecond,
} from '~/utils/format'
import { useDateNow } from '~/composables/useDateNow'

import type { NodeStatus } from '~/types/generated'
import NodeDetailsHeader from '~/components/NodeDetails/NodeDetailsHeader.vue'
import BlockLink from '~/components/molecules/BlockLink.vue'
import Accordion from '~/components/Accordion.vue'
import NodeLink from '~/components/molecules/NodeLink.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import DescriptionList from '~/components/atoms/DescriptionList.vue'
import DescriptionListItem from '~/components/atoms/DescriptionListItem.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import WarningIcon from '~/components/icons/WarningIcon.vue'
import NotFound from '~/components/molecules/NotFound.vue'
const { NOW } = useDateNow()

type Props = {
	node: NodeStatus
}

defineProps<Props>()
</script>
~~/src/utils/translateValidationCommittee
