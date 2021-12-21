Ideally the test files should be colocated with the components. However, there is an issue in Nuxt 3, where the test files are included in the production bundle. This is an issue, since the Jest globals will then be undefined, and the entire bundle will fail.

According to the JSDoc hints (as well as the documentation), any files with a `**.spec.*` or `**.test.*` files are automatically ignored, though this seems not to be the case – even if we ignore the files explicitly in `nuxt.config.ts` or add a `.nuxtignore`.

Once this issue has been resolved, we can move the test back to be colocated.
