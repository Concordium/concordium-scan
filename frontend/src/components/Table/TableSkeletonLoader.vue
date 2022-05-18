<template>
	<div class="relative">
		<Table :class="$style.pauseOverlay">
			<TableHead>
				<TableRow>
					<TableTh width="25%"
						><div :class="$style.pauseOverlayBar">&nbsp;</div></TableTh
					>
					<TableTh width="25%"
						><div :class="$style.pauseOverlayBar">&nbsp;</div></TableTh
					>
					<TableTh v-if="breakpoint >= Breakpoint.MD" width="25%"
						><div :class="$style.pauseOverlayBar">&nbsp;</div></TableTh
					>
					<TableTh v-if="breakpoint >= Breakpoint.SM" width="25%">
						<div :class="$style.pauseOverlayBar">&nbsp;</div>
					</TableTh>
				</TableRow>
			</TableHead>

			<TableRow v-for="n in 10" :key="n">
				<TableTd v-for="kn in 4" :key="kn">
					<div :class="$style.pauseOverlayBar">&nbsp;</div>
				</TableTd>
			</TableRow>
		</Table>
		<div class="absolute top-1/4 left-1/3 z-10">
			<h1 class="animate-pulse"><PauseIcon class="h-56 w-56" /></h1>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import PauseIcon from '~/components/icons/PauseIcon.vue'
const { breakpoint } = useBreakpoint()
</script>
<style module>
.pauseOverlay {
	min-height: 527px;
}
.pauseOverlayBar {
	@apply px-6 py-3 overflow-hidden;
	background-color: #2c284d;
	height: 14px;
	line-height: 32px;
	border-radius: 7px;
	width: 80%;
	&:after {
		position: absolute;
		transform: translateY(-50%);
		top: 50%;
		left: 0;
		content: '';
		display: block;
		width: 100%;
		height: 24px;
		background-image: linear-gradient(
			100deg,
			rgba(255, 255, 255, 0),
			rgba(255, 255, 255, 0.5) 60%,
			rgba(255, 255, 255, 0) 80%
		);
		background-size: 200px 24px;
		background-position: -100px 0;
		background-repeat: no-repeat;
		animation: loading 1s infinite;
	}
}
</style>
