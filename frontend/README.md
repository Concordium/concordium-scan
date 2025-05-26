# CCDScan frontend

The frontend of CCDScan is a server-side rendered single page app, which consumes data via [GraphQL](https://graphql.org/) from the backend.

The frontend is built on some fundamental technologies:

- **[Vue](https://vuejs.org/)**
  JavaScript framework for building user interfaces for the web. It is reactive, declarative and very approachable to build and extend.
- **[Nuxt 3](https://v3.nuxtjs.org/)**
  Application framework built on top of Vue. Out of the box it gives us some things that Vue itself lacks, such as routing, and it comes with a build system supporting code splitting and ohter optimisations.
- **[TypeScript](https://www.typescriptlang.org/)**
  A typed programming language, which compiles to JavaScript. This acts as an accelerator during development, and prevents most type errors at write-time and compile-time. [More on this in a later section](#typescript).

## Setup

**Install dependencies:**

```sh
yarn
```

## Usage

### Run Development server

To run the development server a configuration can be provided using `.env` file, without it will assume the backend API is running locally.

```sh
yarn dev
```

Go to [http://localhost:3000](http://localhost:3000).

To develop against our backend APIs already in production, specify the appropriate file with environment variables. Below is an example of using testnet backend API:

```sh
yarn dev --dotenv .env.testnet
```


### Build and serve locally

You can build and run the production image locally.

To build the image run:

```sh
docker build -t IMAGE_NAME:VERSION .
```

where `IMAGE_NAME` and `VERSION` are some container name and version.


The image can be run against Testnet by providing the `.env.testnet` configuration.

```sh
docker run --public 3000:3000 --env-file .env.testnet IMAGE_NAME:VERSION
```

The application is now available at `http://localhost:3000/`.

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

### Type generation

We are using [GraphQL Code Generator](https://www.graphql-code-generator.com/) to generate types from our GraphQL schema. Whenever there is a change in the schema, you can run the below commands which will generate a new set of types in `types/generated.ts` file. You should not edit this file manually, as the codegen will simply overwrite changes.

- Navigate into the `backend-rust` folder and run:

```
env SQLX_OFFLINE=true cargo run --bin ccdscan-api -- --schema-out ./schema.graphql
```

- Run the command in the `frontend` folder:

```sh
yarn gql-codegen
```

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

- **Vue3 Carousel does not properly export it's types**
  We've "solved" this by declaring the module on our own, but that just hides the problem. See https://github.com/ismail9k/vue3-carousel/issues/10.

- **Tailwind config causes build error**
  When building the application with a RC version of Nuxt, we see an error:

  > Importing directly from a nuxt.config file is not allowed. Instead, use runtime config or a module.

  This error occurs because Tailwind is traversing `nuxt.config` directly into their own config. Overwriting this on our end seems not to work, so for the time being we've reverted to a working version of Nuxt. See https://github.com/nuxt/framework/issues/2886.