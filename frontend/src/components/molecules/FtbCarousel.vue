<template>
	<ClientOnly>
		<div v-if="!isMobile" class="block grid grid-cols-2 mb-20 gap-4">
			<slot></slot>
		</div>
		<carousel v-else :items-to-show="1">
			<template #addons>
				<pagination />
			</template>
			<slot></slot>
		</carousel>
	</ClientOnly>
</template>

<script lang="ts" setup>
import { Carousel, Pagination } from 'vue3-carousel'
const isMobile = ref(false)
onMounted(() => {
	window.addEventListener('resize', updateSize)
	window.addEventListener('orientationchange', updateSize)

	updateSize()
})
onUnmounted(() => {
	window.removeEventListener('resize', updateSize)
	window.removeEventListener('orientationchange', updateSize)
})
const updateSize = () => {
	isMobile.value = window.innerWidth <= 1024
}
</script>

<style module></style>
