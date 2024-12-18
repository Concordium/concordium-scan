<template>
	<aside>
		<h4 class="text-sm text-theme-faded">
			<slot name="title" />
		</h4>
		<h3 class="text-md">
			<slot name="default" /><br />
			<span class="text-violet-500 text-theme-faded">
				<slot name="secondary" />
			</span>
		</h3>
	</aside>
</template>
