<template>
	<span v-if="!event?.decoded?.text || event.decoded.text.trim() === ''">
		Data was registered: <em class="text-gray-500">(empty)</em>
	</span>
	<span v-else-if="event.decoded.decodeType === 'HEX'">
		<div class="flex items-center gap-2 mb-1">
			<span>Data was registered:</span>
			<button
				:class="[
					'px-3 py-1 text-xs font-medium rounded-full transition-colors duration-200 border',
					useDiagnostic
						? 'bg-blue-600 text-white border-blue-600'
						: 'bg-gray-200 text-gray-700 border-gray-300 hover:bg-gray-300',
				]"
				@click="useDiagnostic = !useDiagnostic"
			>
				{{ useDiagnostic ? 'Diagnostic' : 'JSON' }}
			</button>
		</div>
		<div class="overflow-x-auto max-w-full">
			<code
				class="text-xs bg-gray-100 dark:bg-gray-800 p-1 rounded block whitespace-pre-wrap break-all"
				>{{ formatHexData(event.decoded.text, useDiagnostic) }}</code
			>
		</div>
	</span>
	<span v-else-if="event.decoded.decodeType === 'CBOR'">
		<div class="flex items-center gap-2 mb-1">
			<span>Data was registered:</span>
			<button
				:class="[
					'px-3 py-1 text-xs font-medium rounded-full transition-colors duration-200 border',
					useDiagnostic
						? 'bg-blue-600 text-white border-blue-600'
						: 'bg-gray-200 text-gray-700 border-gray-300 hover:bg-gray-300',
				]"
				@click="useDiagnostic = !useDiagnostic"
			>
				{{ useDiagnostic ? 'Diagnostic' : 'JSON' }}
			</button>
		</div>
		<div class="overflow-x-auto max-w-full">
			<pre
				class="text-xs bg-gray-100 dark:bg-gray-800 p-2 rounded whitespace-pre-wrap break-words max-w-full"
				>{{ formatCborData(event.decoded.text, useDiagnostic) }}</pre
			>
		</div>
	</span>
	<span v-else>
		Data was registered ({{ event.decoded.decodeType || 'unknown' }}):<br />
		<div class="overflow-x-auto max-w-full">
			<code
				class="text-xs bg-gray-100 dark:bg-gray-800 p-1 rounded block whitespace-pre-wrap break-all"
				>{{ event.decoded.text }}</code
			>
		</div>
	</span>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { DataRegistered } from '~/types/generated'
import { formatHexData, formatCborData } from '~/utils/format'

type Props = {
	event: DataRegistered
}

defineProps<Props>()

// Reactive state for toggling between JSON and CBOR diagnostic notation
const useDiagnostic = ref(false)
</script>
