<template>
	<div
		:class="[
			$style.tabContainer,
			{
				flex: variant === 'horizontal',
			},
		]"
	>
		<ul
			:class="[
				{
					flex: variant === 'vertical',
				},
			]"
		>
			<li v-for="(tab, index) in tabList" :key="index" :class="$style.tabItem">
				<input
					:id="`${index}-${ID}`"
					v-model="activeTab"
					type="radio"
					:name="`${index}-tab`"
					:value="index + 1"
				/>
				<label :for="`${index}-${ID}`" v-text="tab" />
			</li>
		</ul>
		<template v-for="(_, index) in tabList">
			<div v-if="index + 1 === activeTab" :key="index" :class="$style.tabPanel">
				<slot :name="`tabPanel-${index + 1}`" />
			</div>
		</template>
	</div>
</template>

<script lang="ts" setup>
const ID = `tab-${Math.floor(Math.random() * 1000)}`
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

<style module>
.flex {
	display: flex;
}

.tabContainer {
	margin-bottom: 40px;
}

.tabItem {
	padding: 12px 24px 10px;
	background-color: var(--color-background-elevated);
	opacity: 0.4;
	border-radius: 8px 8px 0 0;
	margin-right: 3px;
	margin-bottom: 3px;
}

.tabItem input {
	display: none;
}

.tabItem:has(input[type='radio']:checked) {
	background-color: var(--color-background-elevated);
	opacity: 1;
	margin-bottom: 0;
}

.tabPanel {
	padding: 20px 20px 10px;
	border-radius: 0 16px 16px;
	background-color: var(--color-background-elevated);
}
</style>
