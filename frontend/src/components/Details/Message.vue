<template>
	<div class="w-full">
		<div>
			Message:
			<InfoTooltip text="Input parameters given to activity entrypoint." />
		</div>
		<div class="flex">
			<code class="truncate w-96">
				{{ message }}
			</code>
			<Modal :header-title="'Message'">
				<template #body>
					<code style="text-align: left">
						<pre>{{ JSON.stringify(JSON.parse(message), null, 2) }}</pre>
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
import Modal from './Modal.vue'

type Props = {
	message: string
}
defineProps<Props>()
</script>
