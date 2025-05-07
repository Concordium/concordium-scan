export default defineNuxtRouteMiddleware(to => {
	const {
		public: { enablePltFeatures },
	} = useRuntimeConfig()

	if (!enablePltFeatures && to.path.startsWith('/stable-coin')) {
		return navigateTo('/') // Redirect to home or show 404
	}
})
