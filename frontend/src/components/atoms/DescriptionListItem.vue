<template>
	<div class="grid grid-cols-2">
		<dt><slot /></dt>
		<dd class="text-right mb-2">
			<slot name="content" />
		</dd>
	</div>
</template>
