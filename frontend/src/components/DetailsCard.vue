<template>
	<aside>
		<h4 class="text-sm text-theme-faded">
			<slot name="title"></slot>
		</h4>
		<h3 class="text-xl">
			<slot name="default"></slot><br />
			<span class="text-violet-500 text-theme-faded">
				<slot name="secondary"></slot>
			</span>
		</h3>
	</aside>
</template>
