# Nuxt TypeScript starter template

A [Nuxt.js](https://github.com/nuxt/nuxt.js) + [@nuxt/typescript](https://github.com/nuxt/typescript) starter project template.

## Setup

**Install dependencies:**

```sh
yarn
```

## Usage

### Run Development server

```sh
yarn dev
```

Go to [http://localhost:3000](http://localhost:3000)

### Build and serve on local Firebase emulator

Some times, you might want to run the build on a local emulated Firebase server. This is useful for debugging issues only occuring on the server. For this, you need to install [Firebase CLI](https://www.npmjs.com/package/firebase-tools) on your machine.

```sh
yarn global add firebase-tools
```

You're now ready to build and serve the app.

```sh
yarn build
firebase emulators:start
```

The app itself will now run on [http://localhost:5000](http://localhost:5000), while the Firebase Emulator UI can be seen on [http://localhost:5000](http://localhost:5000).

## Deployment

The app is currently hosted in [Firebase](https://console.firebase.google.com/), in three different projects (one for each environment). This is the suggested way to do it, because each environment needs its own server (as Nuxt3 does not currently support static builds).

Currently, the app is automatically deployed to DEV and TEST in true CD fashion on any change to `main` (pending successful quality checks). PROD deployment currently requires manual approval, although this is subject to change when we have more automated quality control in place. You can see [the entire CI/CD pipeline](https://dev.azure.com/fintechbuilders/ConcordiumScan/_build?definitionId=15) in Azure.

Please note that the pipeline itself has a single secret variable for the Firebase token.

The pipeline configuration itself can be seen in [azure-pipelines-frontend.yml](devops/azure-pipelines-frontend.yml).

## Quality control

We're using multiple automated quality checks.

### [ESLint](https://eslint.org/)

Static code analysis to enforce certain patterns and idiomatic programming. It is strongly recommended to install this as a plugin in your IDE, so you get warnings and errors "live", although you can also run it manually:

```sh
yarn lint
```

### [Prettier](https://prettier.io/)

Opinionated code formatter to make sure our code follows the same uniform style. It is strongly recommended to install this as a plugin in your IDE, so the code is automatically formatted on save.

### [Typescript](https://www.typescriptlang.org/)

Adding strict type checking to the source code, allows us to avoid silly bugs and to rewrite code with confidence. Some IDE's have native support for Typescript, but if not it is strongly recommended that you install relevant plugins. You can also run it manually:

```sh
yarn typecheck
```

### Unit tests

We're using [Vue Testing Library](https://testing-library.com/docs/vue-testing-library/intro) together with [Jest](https://jestjs.io/) for all unit tests.

The tests will be run as a quality gate in the CI, but you can also run it manually:

```sh
yarn test
```

During development, you can run the tests in watch mode:

```sh
yarn test:watch
```

#### Testing utility

We have a tiny testing utility, which gets rid of some of the boilerplate and makes testing Vue components simpler. It's advised to use this whenever you're testing a Vue component, in order to keep a consistent language and to make migration to a different testing library in the future easy.

Here is how to use it:

```typescript
import Button from './Button.vue'
import { setupComponent, screen, fireEvent } from '~/utils/testing'

const defaultSlots = {
	default: 'Hello',
}

const defaultProps = {
	onClick: () => {
		/* noop */
	},
}

const defaultAttrs = {
	disabled: false,
}

// Setup component with default props, slots and attributes
const { render } = setupComponent(Button, {
	defaultProps,
	defaultSlots,
	defaultAttrs,
})

describe('Button', () => {
	it('can be clicked', () => {
		const onClick = jest.fn()
		const props = { onClick }

		// render button (combining default props with new props)
		render({ props })

		expect(onClick).not.toHaveBeenCalled()

		const button = screen.getByText(defaultSlots.default)

		fireEvent.click(button)

		expect(onClick).toHaveBeenCalledTimes(1)
	})
})
```

### Git hooks

We're using [Husky](https://typicode.github.io/husky/#/) to write our git hooks. We only have one git hook; **pre-commit**. It will make sure, that when committing, the staged code is linted (using ESLint), formatted (using Prettier) and typechecked.

Husky should be installed together with the rest of the dependencies, but if it isn't, you can install it manually:

```sh
npx husky install
```

Furthermore, it might complain that the hook cannot be run. If this is the case, you need to give Husky access to execute the file:

```sh
chmod +x .husky/pre-commit
```

### Type generation

We are using [GraphQL Code Generator](https://www.graphql-code-generator.com/) to generate types from our GraphQL schema. Whenever there is a change in the schema, you can run the following:

```sh
yarn gql-codegen
```

This will generate a new set of types in `types/generated.ts`. You should never edit this file manually, as the codegen will simply overwrite changes.

# Known issues

- **Components using the `@apply` directive from Tailwind can not be tested.**

  It results in a very non-descriptive syntax error. It might be that we manually need to add a Webpack loader for this.

- **vue-tsc seems not to use the tsconfig in git hook.**

  Running the typecheck on Vue files as a pre-commit results in many funny errors related to module resolution. This might also be a configuration error on our side.

- **Icons from @heroicons must be imported explicitly from `index.js`.**

  It seems ESM imports is not very well supported in @heroicons. See https://github.com/tailwindlabs/heroicons/issues/564 and https://github.com/tailwindlabs/heroicons/issues/309. Presumably we could also import the icons one at a time (this might reduce the bundle size).

- **`<teleport>` can't be used yet.**

  We would like to use `<teleport>` for some global components, e.g. drawers. Unfortunately, this causes a hydration error when navigating away from the page. There is [an open issue](https://github.com/nuxt/framework/issues/1907) on it with Nuxt, however it seems not to have very high priority. For now we have solved it by lifting those components up, and then keep track of state with a `useState` composable.

- **Jest sometimes has problems with absolute- and auto imports**

  When running tests, Jest cannot find some components (e.g.) `Button` when they are imported with absolute paths or when they are auto-imported. This is illogical, as it can import other modules just fine (e.g. `Table`).
