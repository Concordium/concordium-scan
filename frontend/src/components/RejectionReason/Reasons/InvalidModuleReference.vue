<template>
	<span>
		Module <ModuleRef :module-ref="reason.moduleRef" /> does not exist
	</span>
</template>

<script setup lang="ts">
import ModuleRef from '~/components/molecules/ModuleRef.vue'
import type { InvalidModuleReference } from '~/types/generated'

type Props = {
	reason: InvalidModuleReference
}

defineProps<Props>()
</script>
