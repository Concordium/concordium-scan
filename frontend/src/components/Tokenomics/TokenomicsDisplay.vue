<template>
	<aside class="md:grid md:gap-6 md:grid-cols-3 mb-12">
		<h4 class="font-bold mb-4"><slot name="title" /></h4>
		<div class="md:col-span-2">
			<slot name="content" />
		</div>
	</aside>
</template>
