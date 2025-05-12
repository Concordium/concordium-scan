<template>
	<TableHead>
		<TableRow>
			<TableTh>Transaction</TableTh>
			<TableTh>Type</TableTh>
			<TableTh>Details</TableTh>
		</TableRow>
	</TableHead>
	<TableBody>
		<TableRow v-for="(tokenEvent, i) in tokenEvents" :key="tokenEvent.tokenId">
			<TableTd>
				<TransactionLink :hash="tokenEvent.transaction!.transactionHash" />
			</TableTd>
			<TableTd>
				{{ tokenEvent.event.__typename?.slice(3, -5) }}
			</TableTd>
			<TableTd>
				<DetailsView
					v-if="tokenEvent.event.__typename === 'CisTokenMetadataEvent'"
					:id="i"
				>
					<CisTokenMetadataEventDetails :event="tokenEvent.event" />
				</DetailsView>
				<DetailsView
					v-if="tokenEvent.event.__typename === 'CisBurnEvent'"
					:id="i"
				>
					<CisBurnEventDetails
						:event="tokenEvent.event"
						:decimals="decimals"
						:symbol="symbol"
					/>
				</DetailsView>
				<DetailsView
					v-if="tokenEvent.event.__typename === 'CisMintEvent'"
					:id="i"
				>
					<CisMintEventDetails
						:event="tokenEvent.event"
						:decimals="decimals"
						:symbol="symbol"
					/>
				</DetailsView>
				<DetailsView
					v-if="tokenEvent.event.__typename === 'CisTransferEvent'"
					:id="i"
				>
					<CisTransferEventDetails
						:event="tokenEvent.event"
						:decimals="decimals"
						:symbol="symbol"
					/>
				</DetailsView>
			</TableTd>
		</TableRow>
	</TableBody>
</template>
<script lang="ts" setup>
import TransactionLink from '../molecules/TransactionLink.vue'
import DetailsView from '../Details/DetailsView.vue'
import CisTokenMetadataEventDetails from './Events/CisTokenMetadataEventDetails.vue'
import CisBurnEventDetails from './Events/CisBurnEventDetails.vue'
import CisMintEventDetails from './Events/CisMintEventDetails.vue'
import CisTransferEventDetails from './Events/CisTransferEventDetails.vue'
import type { Cis2Event } from '~~/src/types/generated'

type Props = {
	tokenEvents: Cis2Event[]
	decimals?: number | undefined
	symbol?: string | undefined
}
defineProps<Props>()
</script>
