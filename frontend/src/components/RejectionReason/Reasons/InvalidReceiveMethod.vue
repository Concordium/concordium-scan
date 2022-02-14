<template>
	<span>
		Receive method '{{ reason.receiveName }}' does not exist in module
		<ModuleRef :module-ref="reason.moduleRef" />
	</span>
</template>

<script setup lang="ts">
import ModuleRef from '~/components/molecules/ModuleRef.vue'
import type { InvalidReceiveMethod } from '~/types/generated'

type Props = {
	reason: InvalidReceiveMethod
}

defineProps<Props>()
</script>
