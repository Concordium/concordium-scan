<template>
	<div>
		<FtbCarousel non-carousel-classes="grid-cols-1">
			<CarouselSlide class="w-full lg:h-full">
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
							v-for="(coin, index) in dataPerStablecoin?.stablecoin?.holding"
							:key="index"
						>
							<TableTd>
								<Tooltip :text="coin.address" text-class="text-theme-body">
									{{ shortenHash(coin.address) }}
								</Tooltip>
								<TextCopy
									:text="coin.address"
									label="Click to copy block hash to clipboard"
									class="h-5 inline align-baseline"
									tooltip-class="font-sans"
								/>
							</TableTd>
							<TableTd>
								{{ coin.holdings[0].quantity }}
							</TableTd>
							<TableTd> {{ coin.holdings[0].percentage.toFixed(2) }}% </TableTd>
							<TableTd>
								<Tooltip
									:text="
										coin.holdings[0].quantity *
										dataPerStablecoin?.stablecoin?.valueInDoller
									"
									text-class="text-theme-body"
								>
									{{
										(
											coin.holdings[0].quantity *
											dataPerStablecoin?.stablecoin?.valueInDoller
										).toFixed(2)
									}}
								</Tooltip>
							</TableTd>
						</TableRow>
					</TableBody>
				</Table>
			</CarouselSlide>
		</FtbCarousel>
	</div>
</template>
<script lang="ts" setup>
import { shortenHash } from '~/utils/format'
import { useStableCoinDashboardList } from '~/queries/useStableCoinDashboardList'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
const { data: dataPerStablecoin } = useStableCoinDashboardList('USDC', 12)
</script>
