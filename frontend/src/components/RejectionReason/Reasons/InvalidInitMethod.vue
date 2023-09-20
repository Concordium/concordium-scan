<template>
	<span>
		Init method '{{ reason.initName }}' does not exist in module
		<ModuleLink :module-reference="reason.moduleRef" />
	</span>
</template>

<script setup lang="ts">
import ModuleLink from '~/components/molecules/ModuleLink.vue'
import type { InvalidInitMethod } from '~/types/generated'

type Props = {
	reason: InvalidInitMethod
}

defineProps<Props>()
</script>
