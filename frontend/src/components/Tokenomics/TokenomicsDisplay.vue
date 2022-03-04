<template>
	<aside class="grid gap-6 md:grid-cols-3 mb-12">
		<h4 class="font-bold"><slot name="title" /></h4>
		<div class="md:col-span-2">
			<slot name="content" />
		</div>
	</aside>
</template>
