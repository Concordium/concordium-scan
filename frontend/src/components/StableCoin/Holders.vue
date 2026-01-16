<template>
	<div>
		<div v-if="pltHolderLoading" class="w-full h-36 text-center">
			<BWCubeLogoIcon class="w-10 h-10 animate-ping mt-8" />
		</div>
		<div v-else>
			<FtbCarousel non-carousel-classes="grid-cols-1">
				<CarouselSlide class="w-full lg:h-full">
					<Filter
						v-model="lastNTransactions"
						class="mb-4"
						:data="holderFilterOptions"
					/>
					<Table>
						<TableHead>
							<TableRow>
								<TableTh width="33%">Account</TableTh>
								<TableTh width="33%">Quantity</TableTh>
								<TableTh width="1%">Percentage</TableTh>
							</TableRow>
						</TableHead>
						<TableBody>
							<TableRow v-for="(coin, index) in pagedData" :key="index">
								<TableTd>
									<AccountLink :address="coin.accountAddress.asString" />
								</TableTd>
								<TableTd>
									<PltAmount
										:value="coin.amount.value"
										:decimals="Number(coin.amount.decimals)"
									/>
								</TableTd>
								<TableTd>
									<PltAmountPercentage
										:value="
											calculatePercentageforBigInt(
												BigInt(coin.amount.value),
												totalSupply
											)
										"
									/>
								</TableTd>
							</TableRow>
						</TableBody>
					</Table>
					<LoadMore
						v-if="pltHolderData?.pltAccountsByTokenId?.pageInfo"
						:page-info="pltHolderData.pltAccountsByTokenId.pageInfo"
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
import { usePltTokenHolderQuery } from '~/queries/usePltTokenHolderQuery'
import type { PltAccountAmount } from '~/types/generated'
import { usePagedData } from '~/composables/usePagedData'

import {
	holderFilterOptions,
	TransactionFilterOption,
} from '~/types/stable-coin'
import { ref, watch } from 'vue'

// Define Props
const props = defineProps<{
	coinId: string
	totalSupply: bigint
}>()

// Loading state
const lastNTransactions = ref(TransactionFilterOption.Top20)

const coinId = props.coinId
const totalSupply = props.totalSupply

const pageSize = ref(lastNTransactions.value)
const maxPageSize = ref(lastNTransactions.value)
const { first, last, after, before, pagedData, addPagedData, loadMore } =
	usePagedData<PltAccountAmount>([], pageSize, maxPageSize)

watch(
	() => lastNTransactions.value,
	newValue => {
		// Update page size and reset pagination when filter changes
		pageSize.value = newValue
		maxPageSize.value = newValue
		first.value = newValue
		after.value = undefined
		before.value = undefined
		pagedData.value = []
	}
)

const { data: pltHolderData, fetching: pltHolderLoading } =
	usePltTokenHolderQuery(coinId, {
		first,
		last,
		after,
		before,
	})

watch(
	() => pltHolderData.value,
	value => {
		if (value?.pltAccountsByTokenId) {
			addPagedData(
				value.pltAccountsByTokenId.nodes || [],
				value.pltAccountsByTokenId.pageInfo
			)
		}
	},
	{ immediate: true }
)
</script>
