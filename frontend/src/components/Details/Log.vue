<template>
	<div v-if="log" class="w-full">
		<div>
			Event:
			<InfoTooltip text="Full event" />
		</div>
		<div class="flex">
			<code class="truncate w-96">
				{{ log }}
			</code>
			<Modal header-title="Event">
				<template #body>
					<code style="text-align: left">
						<pre>{{ JSON.stringify(JSON.parse(log), null, 2) }}</pre>
					</code>
				</template>
			</Modal>
			<TextCopy
				v-if="log"
				:text="log"
				label="Click to copy event to clipboard"
			/>
		</div>
	</div>
</template>
<script lang="ts" setup>
import InfoTooltip from '../atoms/InfoTooltip.vue'
import TextCopy from '../atoms/TextCopy.vue'
import Modal from './Modal.vue'

type Props = {
	log: string
}
defineProps<Props>()
</script>
