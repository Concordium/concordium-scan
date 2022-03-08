<template>
	<ClientOnly>
		<div
			v-if="!isMobile"
			class="block grid mb-20 gap-4"
			:class="props.nonCarouselClasses"
		>
			<slot></slot>
		</div>
		<carousel v-else ref="carouselRef" slide-width="350px" :items-to-show="1">
			<template #addons>
				<pagination />
			</template>
			<slot></slot>
		</carousel>
	</ClientOnly>
</template>

<script lang="ts" setup>
import { Carousel, Pagination } from 'vue3-carousel'
import { useIsMobile } from '~/composables/useIsMobile'
const { isMobile } = useIsMobile()
type Props = {
	nonCarouselClasses?: string
}
const props = defineProps<Props>()
const carouselRef = ref<Carousel>()
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
