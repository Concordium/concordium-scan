<template>
	<div>
		<div v-if="pltEventsLoading" class="w-full h-36 text-center">
			<BWCubeLogoIcon class="w-10 h-10 animate-ping mt-8" />
		</div>
		<div v-else>
			<FtbCarousel non-carousel-classes="grid-cols-1">
				<CarouselSlide class="w-full lg:h-full">
					<Filter
						v-model="lastNTransactions"
						class="mb-4"
						:data="transactionFilterOptions"
					/>
					<Table>
						<TableHead>
							<TableRow>
								<TableTh width="12.5%">Transaction Hash</TableTh>
								<TableTh width="12.5%">Age</TableTh>
								<TableTh width="12.5%">Token Event</TableTh>
								<TableTh width="12.5%">Symbol</TableTh>
								<TableTh width="12.5%">From</TableTh>
								<TableTh width="12.5%">To</TableTh>
								<TableTh width="12.5%">Target</TableTh>
								<TableTh width="12.5%">Amount</TableTh>
							</TableRow>
						</TableHead>
						<TableBody>
							<TableRow v-for="(event, index) in pagedData" :key="index">
								<TableTd>
									<TransactionLink :hash="event.transactionHash ?? ''" />
								</TableTd>
								<TableTd>
									<Tooltip :text="formatTimestamp(event.block.blockSlotTime)">
										{{
											convertTimestampToRelative(event.block.blockSlotTime, NOW)
										}}
									</Tooltip>
								</TableTd>
								<TableTd>
									<span class="text-theme-interactive font-semibold">
										{{
											event.eventType == 'TOKEN_MODULE'
												? event.tokenModuleType
												: event.eventType
										}}
									</span>
								</TableTd>

								<TableTd>
									{{ event.tokenId }}
								</TableTd>
								<TableTd>
									<AccountLink
										:address="
											event.tokenEvent.__typename == 'TokenTransferEvent'
												? event.tokenEvent.from.address.asString
												: ''
										"
									/>
								</TableTd>
								<TableTd>
									<AccountLink
										:address="
											event.tokenEvent.__typename == 'TokenTransferEvent'
												? event.tokenEvent.to.address.asString
												: ''
										"
									/>
								</TableTd>
								<TableTd>
									<AccountLink
										:address="
											event.tokenEvent.__typename == 'BurnEvent' ||
											event.tokenEvent.__typename == 'MintEvent'
												? event.tokenEvent.target.address.asString
												: ''
										"
									/>
								</TableTd>
								<TableTd
									v-if="
										event.tokenEvent.__typename == 'BurnEvent' ||
										event.tokenEvent.__typename == 'MintEvent' ||
										event.tokenEvent.__typename == 'TokenTransferEvent'
									"
								>
									<PltAmount
										:value="event.tokenEvent.amount.value"
										:decimals="Number(event.tokenEvent.amount.decimals)"
									/>
								</TableTd>
							</TableRow>
						</TableBody>
					</Table>
					<LoadMore
						v-if="pltEventsData?.pltEventsByTokenId?.pageInfo"
						:page-info="pltEventsData.pltEventsByTokenId.pageInfo"
						:on-load-more="loadMore"
					/>
				</CarouselSlide>
			</FtbCarousel>
		</div>
	</div>
</template>
<script lang="ts" setup>
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import Filter from '~/components/StableCoin/Filter.vue'
import LoadMore from '~/components/LoadMore.vue'
import {
	TransactionFilterOption,
	transactionFilterOptions,
} from '~/types/stable-coin'
import { usePagedData } from '~/composables/usePagedData'
import type { PltEvent } from '~/types/generated'
import { useDateNow } from '~/composables/useDateNow'

import { usePltEventByIdQuery } from '~/queries/usePltEventsQuery'
import { ref, watch } from 'vue'
const { NOW } = useDateNow()

// Define Props
const props = defineProps<{
	coinId?: string
}>()

const lastNTransactions = ref(TransactionFilterOption.Top20)

const coinId = props.coinId ?? ''

const pageSize = 10
const maxPageSize = 20
const { first, last, after, before, pagedData, addPagedData, loadMore } =
	usePagedData<PltEvent>([], pageSize, maxPageSize)

first.value = lastNTransactions.value

watch(
	() => lastNTransactions.value,
	newValue => {
		// Reset pagination when filter changes
		first.value = newValue
		after.value = undefined
		before.value = undefined
		pagedData.value = []
	}
)

const { data: pltEventsData, loading: pltEventsLoading } = usePltEventByIdQuery(
	coinId,
	{
		first,
		last,
		after,
		before,
	}
)

watch(
	() => pltEventsData.value,
	value => {
		if (value?.pltEventsByTokenId) {
			addPagedData(
				value.pltEventsByTokenId.nodes || [],
				value.pltEventsByTokenId.pageInfo
			)
		}
	},
	{ immediate: true, deep: true }
)
</script>
