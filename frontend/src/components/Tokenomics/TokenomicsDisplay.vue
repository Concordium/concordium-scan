<template>
	<aside class="grid gap-6 grid-cols-3 mb-12">
		<h4><slot name="title" /></h4>
		<div class="col-span-2">
			<slot name="content" />
		</div>
	</aside>
</template>
