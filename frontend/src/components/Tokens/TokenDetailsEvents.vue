<template>
	<TableHead>
		<TableRow>
			<TableTh>Transaction</TableTh>
			<TableTh>Type</TableTh>
			<TableTh>Details</TableTh>
		</TableRow>
	</TableHead>
	<TableBody>
		<TableRow v-for="(tokenEvent, i) in tokenEvents" :key="tokenEvent">
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
					<CisTokenMetadataEvent :event="tokenEvent.event" />
				</DetailsView>
				<DetailsView
					v-if="tokenEvent.event.__typename === 'CisBurnEvent'"
					:id="i"
				>
					<div>From:</div>
					<div>
						<ContractLink
							v-if="
								tokenEvent.event.fromAddress.__typename === 'ContractAddress'
							"
							:address="tokenEvent.event.fromAddress.asString"
							:contract-address-index="tokenEvent.event.fromAddress.index"
							:contract-address-sub-index="
								tokenEvent.event.fromAddress.subIndex
							"
						/>
						<AccountLink
							v-else-if="
								tokenEvent.event.fromAddress.__typename === 'AccountAddress'
							"
							:address="tokenEvent.event.fromAddress.asString"
						/>
					</div>
					<div>Amount:</div>
					<div>
						<TokenAmount
							:amount="tokenEvent.event.tokenAmount"
							:symbol="symbol"
							:fraction-digits="Number(decimals || 0)"
						/>
					</div>
					<div v-if="tokenEvent.event.parsed" class="w-full">
						<div>
							Event:
							<InfoTooltip text="Full event" />
						</div>
						<div class="flex">
							<code class="truncate w-96">
								{{ tokenEvent.event.parsed }}
							</code>
							<Modal header-title="Event">
								<template #body>
									<code style="text-align: left">
										<pre>{{
											JSON.stringify(
												JSON.parse(tokenEvent.event.parsed),
												null,
												2
											)
										}}</pre>
									</code>
								</template>
							</Modal>
							<TextCopy
								v-if="tokenEvent.event.parsed"
								:text="tokenEvent.event.parsed"
								label="Click to copy event to clipboard"
							/>
						</div>
					</div>
				</DetailsView>
			</TableTd>
		</TableRow>
	</TableBody>
</template>
<script lang="ts" setup>
import TransactionLink from '../molecules/TransactionLink.vue'
import DetailsView from '../Details/DetailsView.vue'
import ContractLink from '../molecules/ContractLink.vue'
import AccountLink from '../molecules/AccountLink.vue'
import TokenAmount from '../atoms/TokenAmount.vue'
import InfoTooltip from '../atoms/InfoTooltip.vue'
import Modal from '../Details/Modal.vue'
import TextCopy from '../atoms/TextCopy.vue'
import CisTokenMetadataEvent from './Events/CisTokenMetadataEvent.vue'
import { TokenEvent } from '~~/src/types/generated'

type Props = {
	tokenEvents: TokenEvent[]
	decimals: number | undefined
	symbol: string | undefined
}
defineProps<Props>()
</script>
