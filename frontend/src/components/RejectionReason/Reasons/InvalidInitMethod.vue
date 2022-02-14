<template>
	<span>
		Init method '{{ reason.initName }}' does not exist in module
		<ModuleRef :module-ref="reason.moduleRef" />
	</span>
</template>

<script setup lang="ts">
import ModuleRef from '~/components/molecules/ModuleRef.vue'
import type { InvalidInitMethod } from '~/types/generated'

type Props = {
	reason: InvalidInitMethod
}

defineProps<Props>()
</script>
