<template>
	<header class="flex justify-between p-5">
		<logo-horizontal />
		<nav class="-mr-2">
			<NuxtLink to="/" :class="$style.link">
				<span :class="$style.linkInner">Blocks</span>
			</NuxtLink>
			<NuxtLink to="/transactions" :class="$style.link">
				<span :class="$style.linkInner">Transactions</span>
			</NuxtLink>
			<NuxtLink to="/accounts" :class="$style.link">
				<span :class="$style.linkInner">Accounts</span>
			</NuxtLink>
		</nav>
	</header>
</template>

<style module>
.link {
	@apply text-white font-bold transition-colors py-2 relative;
}

.link:hover {
	color: hsl(var(--color-interactive));
}

.linkInner {
	@apply divide-x border-r-2 px-2;
}

.link:last-child .linkInner {
	@apply border-r-0;
}
</style>

<style>
.router-link-active {
	color: hsl(var(--color-interactive));
}
</style>
