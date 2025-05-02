<template>
	<div>
		<div v-if="holderLoading" class="w-full h-36 text-center">
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
								<TableTh width="25%">Account</TableTh>
								<TableTh width="25%">Quantity</TableTh>
								<TableTh width="25%">Percentage</TableTh>
								<TableTh width="25%">Value</TableTh>
							</TableRow>
						</TableHead>
						<TableBody>
							<TableRow
								v-for="(coin, index) in dataPerStablecoin?.stablecoin?.holdings"
								:key="index"
							>
								<TableTd>
									<AccountLink :address="coin.address" />
								</TableTd>
								<TableTd>
									<Amount :amount="coin.quantity" />
								</TableTd>
								<TableTd> {{ coin.percentage?.toFixed(2) }}% </TableTd>
								<TableTd>
									<Tooltip
										:text="
											String(
												(coin.quantity ?? 0) *
													(dataPerStablecoin?.stablecoin?.valueInDoller ?? 0)
											)
										"
										text-class="text-theme-body"
									>
										${{ numberFormatter(coin?.quantity) }}
									</Tooltip>
								</TableTd>
							</TableRow>
						</TableBody>
					</Table>
				</CarouselSlide>
			</FtbCarousel>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { numberFormatter } from '~/utils/format'
import { useStableCoinDashboardList } from '~/queries/useStableCoinDashboardList'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import Filter from '~/components/StableCoin/Filter.vue'
import {
	TransactionFilterOption,
	transactionFilterOptions,
} from '~/types/stable-coin'

// Define Props
const props = defineProps<{
	coinId?: string
}>()

// Loading state
const isLoading = ref(true)
const lastNTransactions = ref(TransactionFilterOption.Top20)
const limit = ref(20)

const coinId = props.coinId?.toUpperCase() ?? 'USDC'

// Fetch Data

const { data: dataPerStablecoin, fetching: holderLoading } =
	useStableCoinDashboardList(coinId, limit, lastNTransactions)

// Watch for data updates
watch(dataPerStablecoin, newData => {
	if (newData) {
		isLoading.value = false
	}
})
</script>
