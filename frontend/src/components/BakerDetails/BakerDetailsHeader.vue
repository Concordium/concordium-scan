<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<BakerIcon class="w-12 h-12 mr-4 hidden md:block" />

		<div class="flex flex-wrap flex-grow w-1/2">
			<div class="flex flex-col">
				<h3 class="w-full text-sm text-theme-faded">Validator</h3>

				<h1 class="inline-block text-2xl numerical">
					{{ baker.bakerId }}
				</h1>
			</div>
			<div class="flex items-center">
				<Badge
					v-if="
						computedBadgeOptions &&
						baker.state.__typename === 'RemovedBakerState'
					"
					:type="computedBadgeOptions[0]"
				>
					{{ computedBadgeOptions[1] }}
				</Badge>
			</div>
		</div>
	</DrawerTitle>
</template>

<script lang="ts" setup>
import { computed } from 'vue'
import Badge from '~/components/Badge.vue'
import BakerIcon from '~/components/icons/BakerIcon.vue'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import { composeBakerStatus } from '~/utils/composeBakerStatus'
import type { Baker } from '~/types/generated'

type Props = {
	baker: Baker
}

const props = defineProps<Props>()

const computedBadgeOptions = computed(() => composeBakerStatus(props.baker))
</script>
