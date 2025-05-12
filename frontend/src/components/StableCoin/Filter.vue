<template>
	<header class="flex flex-col items-end">
		<div class="flex flex-row items-center">
			<div
				v-for="(item, index) in data"
				:key="index"
				class="cursor-pointer px-3 py-1 text-xs uppercase text-white text-center"
				:class="{
					'rounded-l-lg': index === 0,
					'rounded-r-lg': data && index === data.length - 1,
					'bg-theme-background-primary-elevated': refVal !== item.value,
					'bg-theme-background-interactive': refVal === item.value,
				}"
				@click="handleOnChange(item.value)"
			>
				{{ item.label }}
			</div>
		</div>
	</header>
</template>

<script lang="ts" setup>
import { defineProps, toRef } from 'vue'

const props = defineProps<{
	modelValue: string | number
	data?: { label: string; value: string | number }[]
}>()
const refVal = toRef(props, 'modelValue')

const emit = defineEmits(['update:modelValue'])
const handleOnChange = (event: string | number) => {
	emit('update:modelValue', event)
}
</script>
