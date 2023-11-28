<template>
	<button
		class="transition-colors text-theme-faded hover:text-theme-interactiveHover inline"
		style="width: 24px"
		@click="toggleModalVisible"
	>
		<OverlayIcon class="inline align-text-top h-4" />
	</button>
	<transition name="modal-fade">
		<span v-if="isVisible">
			<div class="modal-backdrop">
				<div class="modal">
					<header class="modal-header">
						<h3 class="text-2xl">
							{{ props.headerTitle }}
						</h3>
						<button type="button" @click="closeModal">
							<XIcon class="h-6" />
						</button>
					</header>
					<section class="modal-body">
						<slot name="body" />
					</section>
				</div>
			</div>
		</span>
	</transition>
</template>
<script setup lang="ts">
import { XIcon } from '@heroicons/vue/solid'
import OverlayIcon from '../icons/OverlayIcon.vue'

type Props = {
	headerTitle: string
}
const props = defineProps<Props>()

const isVisible = ref(false)
const toggleModalVisible = () => {
	isVisible.value = !isVisible.value
}
const closeModal = () => {
	isVisible.value = false
}
</script>
<style>
.modal-body pre {
	white-space: pre-wrap; /* Since CSS 2.1 */
	white-space: -moz-pre-wrap; /* Mozilla, since 1999 */
	white-space: -pre-wrap; /* Opera 4-6 */
	white-space: -o-pre-wrap; /* Opera 7 */
	word-wrap: break-word; /* Internet Explorer 5.5+ */
}

.modal-backdrop {
	top: 0;
	bottom: 0;
	left: 0;
	right: 0;
	width: 100%;
	height: 100%;
	z-index: 100;
	background-color: rgba(0, 0, 0, 0.8);
	position: fixed;
	display: flex;
	justify-content: center;
	align-items: center;
}

.modal {
	background-color: var(--color-background-elevated-nontrans);
	width: 1000px;
	height: 90%;
	border-radius: 16px;
	box-shadow: 0px 0px 50px 0px rgba(0, 0, 0, 0.5);
	overflow-x: auto;
	display: flex;
	flex-direction: column;
}

.modal-header {
	display: flex;
	flex-direction: row;
	padding: 30px;
	justify-content: space-between;

	@media screen and (max-width: 640px) {
		padding: 20px;
	}
}

.modal-body {
	height: 76vh;
	overflow-x: hidden;
	overflow-y: scroll;
	color: #4aae9b;
	padding: 0 40px 40px;

	@media screen and (max-width: 640px) {
		padding: 0 10px 10px;
	}
}
</style>
