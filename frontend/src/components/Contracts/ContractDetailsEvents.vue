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
				<TableRow v-for="contractEvent in contractEvents" :key="contractEvent">
					<TableTd class="numerical">
						<TransactionLink :hash="contractEvent.transactionHash" />
					</TableTd>
					<TableTd>
						<Tooltip
							:text="
								convertTimestampToRelative(contractEvent.blockSlotTime, NOW)
							"
						>
							{{ formatTimestamp(contractEvent.blockSlotTime) }}
						</Tooltip>
					</TableTd>
					<TableTd>
						{{ contractEvent.event.__typename }}
					</TableTd>
					<TableTd>
						<div
							v-if="contractEvent.event.__typename === 'ContractInitialized'"
						>
							<p>Amount:</p>
							<p>
								<Amount :amount="contractEvent.event.amount" />
							</p>
							<br />
							<p>Contract Address:</p>
							<p>
								<ContractLink
									:address="contractEvent.event.contractAddress.asString"
									:contract-address-index="
										contractEvent.event.contractAddress.index
									"
									:contract-address-sub-index="
										contractEvent.event.contractAddress.subIndex
									"
								/>
							</p>
							<br />
							<p>Init Name:</p>
							<p>
								{{ contractEvent.event.initName }}
							</p>
							<br />
							<p>Module Reference:</p>
							<p>
								<ModuleLink :module-reference="contractEvent.event.moduleRef" />
							</p>
							<br />
							<div v-if="contractEvent.event.version">
								<p>Version (nullable):</p>
								<p>
									{{ contractEvent.event.version }}
								</p>
							</div>
							<br />
							<div>
								<p>Logs as HEX:</p>
								<ul v-if="contractEvent.event.eventsAsHex?.nodes?.length">
									<li
										v-for="(event, i) in contractEvent.event.eventsAsHex.nodes"
										:key="i"
										style="list-style-type: circle"
										:class="$style.listItem"
									>
										{{ event }}
									</li>
								</ul>
							</div>
						</div>
						<div v-if="contractEvent.event.__typename === 'ContractUpdated'">
							<p>Amount:</p>
							<p>
								<Amount :amount="contractEvent.event.amount" />
							</p>
							<br />
							<p>Contract Address:</p>
							<p>
								<ContractLink
									:address="contractEvent.event.contractAddress.asString"
									:contract-address-index="
										contractEvent.event.contractAddress.index
									"
									:contract-address-sub-index="
										contractEvent.event.contractAddress.subIndex
									"
								/>
							</p>
							<br />
							<p>Receive Name:</p>
							<p>
								{{ contractEvent.event.receiveName }}
							</p>
							<br />
							<p>Message as HEX:</p>
							<p>
								{{ contractEvent.event.messageAsHex }}
							</p>
							<br />
							<p>Instigator:</p>
							<p>
								<ContractLink
									v-if="
										contractEvent.event.instigator.__typename ===
										'ContractAddress'
									"
									:address="contractEvent.event.instigator.asString"
									:contract-address-index="contractEvent.event.instigator.index"
									:contract-address-sub-index="
										contractEvent.event.instigator.subIndex
									"
								/>
								<AccountLink
									v-else-if="
										contractEvent.event.instigator.__typename ===
										'AccountAddress'
									"
									:address="contractEvent.event.instigator.asString"
								/>
							</p>
							<br />
							<div v-if="contractEvent.event.version">
								<p>Version (nullable):</p>
								<p>
									{{ contractEvent.event.version }}
								</p>
							</div>
							<br />
							<div>
								<p>Event Logs as HEX:</p>
								<ul v-if="contractEvent.event.eventsAsHex?.nodes?.length">
									<li
										v-for="(event, i) in contractEvent.event.eventsAsHex.nodes"
										:key="i"
										style="list-style-type: circle"
									>
										{{ event }}
									</li>
								</ul>
							</div>
						</div>
						<div
							v-if="contractEvent.event.__typename === 'ContractModuleDeployed'"
						>
							<p>Module Reference:</p>
							<p>
								<ModuleLink :module-reference="contractEvent.event.moduleRef" />
							</p>
						</div>
						<div v-else-if="contractEvent.event.__typename === 'ContractCall'">
							<p>Called Contract Update Details</p>
							<br />
							<div>
								<p>Amount:</p>
								<p>
									<Amount
										:amount="contractEvent.event.contractUpdated.amount"
									/>
								</p>
								<br />
								<p>Contract Address:</p>
								<p>
									<ContractLink
										:address="
											contractEvent.event.contractUpdated.contractAddress
												.asString
										"
										:contract-address-index="
											contractEvent.event.contractUpdated.contractAddress.index
										"
										:contract-address-sub-index="
											contractEvent.event.contractUpdated.contractAddress
												.subIndex
										"
									/>
								</p>
								<br />
								<p>Receive Name:</p>
								<p>
									{{ contractEvent.event.contractUpdated.receiveName }}
								</p>
								<br />
								<p>Message as HEX:</p>
								<p>
									{{ contractEvent.event.contractUpdated.messageAsHex }}
								</p>
								<br />
								<p>Instigator:</p>
								<p>
									<ContractLink
										v-if="
											contractEvent.event.contractUpdated.instigator
												.__typename === 'ContractAddress'
										"
										:address="
											contractEvent.event.contractUpdated.instigator.asString
										"
										:contract-address-index="
											contractEvent.event.contractUpdated.instigator.index
										"
										:contract-address-sub-index="
											contractEvent.event.contractUpdated.contractAddress
												.subIndex
										"
									/>
									<AccountLink
										v-else-if="
											contractEvent.event.contractUpdated.instigator
												.__typename === 'AccountAddress'
										"
										:address="
											contractEvent.event.contractUpdated.instigator.asString
										"
									/>
								</p>
								<br />
								<div v-if="contractEvent.event.contractUpdated.version">
									<p>Version (nullable):</p>
									<p>
										{{ contractEvent.event.contractUpdated.version }}
									</p>
								</div>
								<br />
								<div>
									<p>Event Logs as HEX:</p>
									<ul
										v-if="
											contractEvent.event.contractUpdated.eventsAsHex?.nodes
												?.length
										"
									>
										<li
											v-for="(event, i) in contractEvent.event.contractUpdated
												.eventsAsHex.nodes"
											:key="i"
											style="list-style-type: circle"
										>
											{{ event }}
										</li>
									</ul>
								</div>
							</div>
						</div>
						<div v-if="contractEvent.event.__typename === 'ContractUpgraded'">
							<p>Contract Address which was updated</p>
							<p>
								<ContractLink
									:address="contractEvent.event.contractAddress.asString"
									:contract-address-index="
										contractEvent.event.contractAddress.index
									"
									:contract-address-sub-index="
										contractEvent.event.contractAddress.subIndex
									"
								/>
							</p>
							<br />
							<p>From Module</p>
							<p><ModuleLink :module-reference="contractEvent.event.from" /></p>
							<br />
							<p>To Module</p>
							<p><ModuleLink :module-reference="contractEvent.event.to" /></p>
							<br />
						</div>
						<div
							v-if="contractEvent.event.__typename === 'ContractInterrupted'"
						>
							<p>Contract Address which was interrupted</p>
							<p>
								<ContractLink
									:address="contractEvent.event.contractAddress.asString"
									:contract-address-index="
										contractEvent.event.contractAddress.index
									"
									:contract-address-sub-index="
										contractEvent.event.contractAddress.subIndex
									"
								/>
							</p>
							<br />
							<div>
								<p>Logs as HEX:</p>
								<ul v-if="contractEvent.event.eventsAsHex?.nodes?.length">
									<li
										v-for="(event, i) in contractEvent.event.eventsAsHex.nodes"
										:key="i"
										style="list-style-type: circle"
										:class="$style.listItem"
									>
										{{ event }}
									</li>
								</ul>
							</div>
						</div>
						<div v-if="contractEvent.event.__typename === 'ContractResumed'">
							<p>Contract Address which was resumed</p>
							<p>
								<ContractLink
									:address="contractEvent.event.contractAddress.asString"
									:contract-address-index="
										contractEvent.event.contractAddress.index
									"
									:contract-address-sub-index="
										contractEvent.event.contractAddress.subIndex
									"
								/>
							</p>
							<p>Succeeded resume</p>
							<p>{{ contractEvent.event.success }}</p>
						</div>
						<div v-if="contractEvent.event.__typename === 'Transferred'">
							<p>Amount:</p>
							<p>
								<Amount :amount="contractEvent.event.amount" />
							</p>
							<p>From:</p>
							<p>
								<ContractLink
									v-if="
										contractEvent.event.from.__typename === 'ContractAddress'
									"
									:address="contractEvent.event.from.asString"
									:contract-address-index="contractEvent.event.from.index"
									:contract-address-sub-index="
										contractEvent.event.from.subIndex
									"
								/>
								<AccountLink
									v-else-if="
										contractEvent.event.from.__typename === 'AccountAddress'
									"
									:address="contractEvent.event.from.asString"
								/>
							</p>
							<p>To:</p>
							<p>
								<ContractLink
									v-if="contractEvent.event.to.__typename === 'ContractAddress'"
									:address="contractEvent.event.to.asString"
									:contract-address-index="contractEvent.event.to.index"
									:contract-address-sub-index="contractEvent.event.to.subIndex"
								/>
								<AccountLink
									v-else-if="
										contractEvent.event.to.__typename === 'AccountAddress'
									"
									:address="contractEvent.event.to.asString"
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
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import ModuleLink from '~/components/molecules/ModuleLink.vue'
import Amount from '~/components/atoms/Amount.vue'

import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import { ContractEvent, PageInfo } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import { PaginationTarget } from '~~/src/composables/usePagination'

const { NOW } = useDateNow()

type Props = {
	contractEvents: ContractEvent[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
