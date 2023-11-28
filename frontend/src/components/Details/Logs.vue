<template>
	<div class="w-full">
		<div>
			Logs:
			<InfoTooltip text="Logs produced by contract execution." />
		</div>
		<template v-if="events?.nodes?.length">
			<div v-for="(event, i) in events.nodes" :key="i" class="flex">
				<code class="truncate w-96">
					{{ event }}
				</code>
				<Modal :header-title="'Log'">
					<template #body>
						<code style="text-align: left">
							<pre>{{ JSON.stringify(JSON.parse(event), null, 2) }}</pre>
						</code>
					</template>
				</Modal>
				<TextCopy
					:text="event"
					label="Click to copy events logs to clipboard"
				/>
			</div>
		</template>
	</div>
</template>
<script lang="ts" setup>
import { Maybe, StringConnection } from '../../types/generated'
import TextCopy from '../../components/atoms/TextCopy.vue'
import InfoTooltip from '../atoms/InfoTooltip.vue'
import Modal from './Modal.vue'

type Props = {
	events?: Maybe<StringConnection>
}
defineProps<Props>()
</script>
