<template>
	<span>
		Receive method '{{ reason.receiveName }}' does not exist in module
		<ModuleLink :module-reference="reason.moduleRef" />
	</span>
</template>

<script setup lang="ts">
import ModuleLink from '~/components/molecules/ModuleLink.vue'
import type { InvalidReceiveMethod } from '~/types/generated'

type Props = {
	reason: InvalidReceiveMethod
}

defineProps<Props>()
</script>
