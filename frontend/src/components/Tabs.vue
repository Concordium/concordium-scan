<template>
	<div
		:class="{
			flex: variant === 'horizontal',
		}"
	>
		<ul
			:class="{
				flex: variant === 'vertical',
			}"
		>
			<li v-for="(tab, index) in tabList" :key="index">
				<label :for="`${index}`" v-text="tab" />
				<input
					:id="`${index}`"
					v-model="activeTab"
					type="radio"
					:name="`${index}-tab`"
					:value="index + 1"
				/>
			</li>
		</ul>

		<template v-for="(_, index) in tabList">
			<div v-if="index + 1 === activeTab" :key="index">
				<slot :name="`tabPanel-${index + 1}`" />
			</div>
		</template>
	</div>
</template>

<script lang="ts" setup>
const activeTab = ref(1)
defineProps({
	tabList: {
		type: Array,
		required: true,
	},
	variant: {
		type: String,
		required: false,
		default: () => 'vertical',
		validator: (value: string): boolean =>
			['horizontal', 'vertical'].includes(value),
	},
})
</script>

<style>
.flex {
	display: flex;
}
</style>
