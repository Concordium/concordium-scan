﻿<template>
	<ClientOnly>
		<div
			v-if="breakpoint >= Breakpoint.LG"
			class="grid mb-10 gap-4"
			:class="props.nonCarouselClasses"
		>
			<slot />
		</div>
		<carousel v-else ref="carouselRef" slide-width="350px" :items-to-show="1">
			<template #addons>
				<pagination />
			</template>
			<slot />
		</carousel>
	</ClientOnly>
</template>

<script lang="ts" setup>
import { Carousel, Pagination } from 'vue3-carousel'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'

const { breakpoint } = useBreakpoint()

type Props = {
	nonCarouselClasses?: string
}
const props = defineProps<Props>()
const carouselRef = ref()
onMounted(() => {
	setTimeout(() => {
		if (carouselRef.value) carouselRef.value.restartCarousel()
	}, 200)
})
</script>

<style>
.carousel__pagination-button {
	background-color: hsla(var(--color-interactive), 40%);
	width: 10px;
	height: 10px;
	border-radius: 100%;
}
.carousel__pagination-button--active {
	background-color: hsl(var(--color-interactive));
}
</style>
