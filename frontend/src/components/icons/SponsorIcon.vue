<template>
	<SVG
		stroke="currentColor"
		fill="currentColor"
		stroke-width="0"
		viewBox="0 0 24 24"
		height="1.1em"
		width="1.1em"
		class="sponsor-icon"
		:class="{ glow: glowOn }"
	>
		<g>
			<path
				d="M12.5 10.5L16.4985 13.8321C17.5085 14.6738 17.4456 16.2443 16.3715 17.0025L11.2641 20.6077C10.5197 21.1331 9.51435 21.0891 8.81883 20.5005L3.5 16H1V7H5"
				stroke="#212121"
				stroke-width="1.5"
				stroke-linecap="round"
				stroke-linejoin="round"
			/>
			<path
				d="M13.5 9.50003L10.1193 12.1294C9.47992 12.6268 8.57018 12.5701 7.99735 11.9973C7.42684 11.4268 7.36805 10.5216 7.85999 9.88211L11.201 5.53873C11.6862 4.90793 12.4993 4.62485 13.2713 4.81786L18 6.00004H23V15H21"
				stroke="#212121"
				stroke-width="1.5"
				stroke-linecap="round"
				stroke-linejoin="round"
			/>
			<path
				d="M12.5 19.4658L11.5 18.5"
				stroke="#212121"
				stroke-width="1.5"
				stroke-linecap="round"
				stroke-linejoin="round"
			/>
			<path
				d="M15 17.4999L13.5 15.9999"
				stroke="#212121"
				stroke-width="1.5"
				stroke-linecap="round"
				stroke-linejoin="round"
			/>
		</g>
	</SVG>
</template>

<script lang="ts" setup>
import SVG from '~/components/atoms/SVG.vue'
defineProps({
	glowOn: {
		type: Boolean,
		default: false,
	},
})
</script>
<style scoped>
.sponsor-icon {
	transform-origin: center center;
}

.sponsor-icon.glow {
	animation: highlight 2s ease-in-out infinite;
}

@keyframes highlight {
	0% {
		transform: scale(1);
		filter: drop-shadow(0 0 0px currentColor);
		color: currentColor;
	}

	50% {
		transform: scale(1.12);
		filter: drop-shadow(0 0 6px #2bb792);
		color: #2bb792;
	}

	100% {
		transform: scale(1);
		filter: drop-shadow(0 0 0px currentColor);
		color: currentColor;
	}
}
</style>
