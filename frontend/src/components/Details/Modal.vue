<template>
	<button
		class="relative transition-colors text-theme-faded hover:text-theme-interactiveHover inline"
		style="width: 24px"
		@click="toggleModalVisible"
	>
		<ArrowsPointingOut class="inline align-text-top h-4" />
	</button>
	<transition name="modal-fade">
		<span v-if="isVisible">
			<div class="modal-backdrop">
				<div class="modal">
					<section class="modal-body">
						<slot name="body" />
					</section>
					<footer class="modal-footer">
						<button type="button" class="btn-green" @click="closeModal">
							Close Modal
						</button>
					</footer>
				</div>
			</div>
		</span>
	</transition>
</template>
<script setup lang="ts">
import ArrowsPointingOut from '~/components/icons/ArrowsPointingOut.vue'

const isVisible = ref(false)
const toggleModalVisible = () => {
	isVisible.value = !isVisible.value
}
const closeModal = () => {
	isVisible.value = false
}
</script>
<style>
.modal-fade-enter-from,
.modal-fade-leave-to {
	opacity: 0;
}
.modal-fade-enter-active,
.modal-fade-leave-active {
	transition: opacity 0.5s ease;
}
.modal-fade-enter-to,
.modal-fade-leave-from {
	opacity: 1;
}
.modal-backdrop {
	position: fixed;
	top: 0;
	bottom: 0;
	left: 0;
	right: 0;
	display: flex;
	justify-content: center;
	align-items: center;
}

.modal {
	background-color: hsla(245, 24%, 51%, 100%);
	/* background-color: var(--color-background-elevated); // Doesn't work */
	opacity: 1;
	border: 1px solid #787594;
	box-shadow: 0px 0px 15px 0px rgba(0, 0, 0, 0.2);
	overflow-x: auto;
	display: flex;
	flex-direction: column;
}

.modal-footer {
	padding: 15px;
	display: flex;
}

.modal-footer {
	border-top: 1px solid #eeeeee;
	flex-direction: column;
	justify-content: flex-end;
}

.modal-body {
	color: #4aae9b;
	position: relative;
	padding: 20px 10px;
}

.btn-green {
	color: white;
	background: #4aae9b;
	border: 1px solid #4aae9b;
	border-radius: 2px;
}
</style>
