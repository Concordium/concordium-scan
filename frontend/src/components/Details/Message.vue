<template>
	<div class="w-full">
		<div>
			Message:
			<InfoTooltip text="Input parameters given to activity entrypoint." />
		</div>
		<div class="flex">
			<code class="truncate w-36">
				{{ message }}
			</code>
			<Tooltip text="">
				<ArrowsPointingOut class="inline align-text-top h-4" />
				<template #content>
					<code style="text-align: left">
						<pre style="white-space: pre-wrap">
							{{ JSON.stringify(JSON.parse(message), null, 2) }}
						</pre
						>
					</code>
				</template>
			</Tooltip>
			<Modal>
				<template #body>
					<code style="text-align: left">
						<pre style="white-space: pre-wrap">
							{{ JSON.stringify(JSON.parse(message), null, 2) }}
						</pre
						>
					</code>
				</template>
			</Modal>
			<TextCopy
				v-if="message"
				:text="message"
				label="Click to copy message to clipboard"
			/>
		</div>
	</div>
</template>
<script lang="ts" setup>
import InfoTooltip from '../atoms/InfoTooltip.vue'
import TextCopy from '../atoms/TextCopy.vue'
import Tooltip from '../atoms/Tooltip.vue'
import ArrowsPointingOut from '../icons/ArrowsPointingOut.vue'
import Modal from './Modal.vue'

type Props = {
	message: string
}
defineProps<Props>()
</script>
