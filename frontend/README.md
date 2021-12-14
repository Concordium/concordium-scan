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

### Build/Run SSR enabled application

```sh
yarn build
yarn start
```

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
